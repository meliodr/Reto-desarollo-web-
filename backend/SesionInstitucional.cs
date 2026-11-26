using System.Security.Claims;
using Npgsql;

namespace Sisplan.Api;

internal sealed record ModulosInstitucionales(bool Pei, bool Poa);
internal sealed record InstitucionAccesible(Guid Id, string Nombre, string Rol, Guid? DependenciaId, ModulosInstitucionales Modulos)
{
    internal bool LimitadaAlArea => Rol is "ResponsableArea" or "DirectorArea";
}
internal sealed record RequiereModulo(bool Pei = false, bool Poa = false);

// Cada solicitud dispone de una conexión, desde la comprobación de membresía hasta la última consulta.
// Npgsql reinicia la sesión antes de compartir esa conexión física con otra solicitud.
internal sealed class SesionInstitucional(NpgsqlDataSource pool) : IAsyncDisposable
{
    private NpgsqlConnection? conexion;
    public NpgsqlConnection Conexion => conexion ?? throw new InvalidOperationException("Falta el contexto institucional.");
    public string Emisor { get; private set; } = "";
    public string Sujeto { get; private set; } = "";
    public List<InstitucionAccesible> Instituciones { get; } = [];
    public InstitucionAccesible? Activa { get; private set; }

    public static async Task VerificarCuentaAsync(NpgsqlDataSource pool)
    {
        const string sql = """
            SELECT current_user = 'sisplan_app' AND NOT rolsuper AND NOT rolbypassrls
               AND NOT EXISTS (SELECT FROM pg_roles r WHERE (r.rolsuper OR r.rolbypassrls)
                               AND pg_has_role(current_user, r.oid, 'MEMBER'))
               AND NOT EXISTS (SELECT FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
                               WHERE n.nspname = 'sisplan' AND c.relkind IN ('r','p')
                                 AND pg_has_role(current_user, c.relowner, 'MEMBER'))
            FROM pg_roles WHERE rolname = current_user;
            """;
        await using var command = pool.CreateCommand(sql);
        if (await command.ExecuteScalarAsync() is not true)
            throw new InvalidOperationException("La API requiere la cuenta sisplan_app sin propiedad de las tablas ni privilegios de omisión de RLS.");
    }

    public NpgsqlCommand CreateCommand(string sql) => new(sql, Conexion);

    public async Task ResolverAsync(string emisor, string sujeto, Guid? seleccionada, CancellationToken ct)
    {
        Emisor = emisor;
        Sujeto = sujeto;
        conexion = await pool.OpenConnectionAsync(ct);
        // También borra explícitamente la selección antes de enumerar membresías.
        await using (var identity = CreateCommand("""
            SELECT set_config('sisplan.emisor', @emisor, false), set_config('sisplan.sujeto', @sujeto, false),
                   set_config('sisplan.institucion_id', '', false);
            """))
        {
            identity.Parameters.AddWithValue("emisor", emisor);
            identity.Parameters.AddWithValue("sujeto", sujeto);
            await identity.ExecuteNonQueryAsync(ct);
        }

        const string sql = """
            SELECT i.id, i.nombre, m.rol, m.dependencia_id, i.pei_habilitado, i.poa_habilitado
            FROM sisplan.membresias_actuales() m
            JOIN sisplan.institucion i ON i.id = m.institucion_id
            ORDER BY i.id;
            """;
        await using (var memberships = CreateCommand(sql))
        {
            await using var reader = await memberships.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                Instituciones.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetGuid(3), new(reader.GetBoolean(4), reader.GetBoolean(5))));
        }

        Activa = seleccionada.HasValue ? Instituciones.Find(i => i.Id == seleccionada.Value) : Instituciones.FirstOrDefault();
        if (Activa is null)
            return;

        await using var activate = CreateCommand("SELECT set_config('sisplan.institucion_id', @id, false)");
        activate.Parameters.AddWithValue("id", Activa.Id.ToString("D"));
        await activate.ExecuteNonQueryAsync(ct);
    }

    public ValueTask DisposeAsync() => conexion?.DisposeAsync() ?? ValueTask.CompletedTask;
}

internal sealed class ContextoInstitucionalMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, SesionInstitucional sesion)
    {
        if (!context.Request.Path.StartsWithSegments("/api/v1") || context.GetEndpoint() is null)
        {
            await next(context);
            return;
        }

        var emisor = context.User.FindFirstValue("iss");
        var sujeto = context.User.FindFirstValue("sub");
        if (context.User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(emisor) || string.IsNullOrWhiteSpace(sujeto))
        {
            await Results.Challenge().ExecuteAsync(context);
            return;
        }

        Guid? seleccionada = null;
        if (context.Request.Headers.TryGetValue("X-Institucion-Id", out var header))
        {
            if (header.Count != 1 || !Guid.TryParseExact(header[0], "D", out var id))
            {
                await Results.Problem(statusCode: 400, title: "X-Institucion-Id debe ser un UUID.").ExecuteAsync(context);
                return;
            }
            seleccionada = id;
        }

        await sesion.ResolverAsync(emisor, sujeto, seleccionada, context.RequestAborted);
        if (sesion.Activa is not { } activa)
        {
            await Results.Problem(statusCode: 403, title: "La cuenta carece de acceso a la institución solicitada.").ExecuteAsync(context);
            return;
        }
        var modulos = context.GetEndpoint()!.Metadata.GetOrderedMetadata<RequiereModulo>();
        if (modulos.Any(m => (m.Pei && !activa.Modulos.Pei) || (m.Poa && !activa.Modulos.Poa)))
        {
            await Results.Problem(statusCode: 403, title: "La institución no tiene habilitado el módulo requerido.").ExecuteAsync(context);
            return;
        }
        await next(context);
    }
}
