using NpgsqlTypes;

namespace Sisplan.Api;

internal static class InstitucionEndpoints
{
    private sealed record Dependencia(Guid Id, string Nombre, bool Activa);
    private sealed record Periodo(Guid Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin);
    private sealed record Unidad(string Codigo, string Nombre);
    private sealed record Evento(long Id, Guid InstitucionId, string Emisor, string Sujeto,
        string Accion, string Entidad, string Referencia, DateTime Fecha);

    public static void MapInstitucional(this RouteGroupBuilder api)
    {
        api.MapGet("/sesion", (SesionInstitucional sesion) => Results.Ok(new
        {
            Actor = new { sesion.Emisor, sesion.Sujeto },
            sesion.Instituciones,
            InstitucionActivaId = sesion.Activa!.Id
        }));
        api.MapGet("/organizacion", OrganizacionAsync);
        api.MapGet("/auditoria", AuditoriaAsync).RequireAuthorization("ConsultarAuditoria");
    }

    private static async Task<IResult> OrganizacionAsync(SesionInstitucional sesion, CancellationToken ct)
    {
        const string sql = """
            SELECT id, nombre, activa FROM sisplan.dependencia
            WHERE institucion_id = @institucion AND (@area IS NULL OR id = @area) ORDER BY nombre, id;
            SELECT id, nombre, fecha_inicio, fecha_fin FROM sisplan.periodo
            WHERE institucion_id = @institucion ORDER BY fecha_inicio, id;
            SELECT codigo, nombre FROM sisplan.unidad_medida WHERE institucion_id = @institucion ORDER BY codigo;
            """;
        var activa = sesion.Activa!;
        await using var command = sesion.CreateCommand(sql);
        command.Parameters.AddWithValue("institucion", activa.Id);
        command.Parameters.AddWithValue("area", NpgsqlDbType.Uuid,
            (object?)(activa.LimitadaAlArea ? activa.DependenciaId : null) ?? DBNull.Value);
        var dependencias = new List<Dependencia>();
        var periodos = new List<Periodo>();
        var unidadesMedida = new List<Unidad>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            dependencias.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetBoolean(2)));
        await reader.NextResultAsync(ct);
        while (await reader.ReadAsync(ct))
            periodos.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetFieldValue<DateOnly>(2), reader.GetFieldValue<DateOnly>(3)));
        await reader.NextResultAsync(ct);
        while (await reader.ReadAsync(ct))
            unidadesMedida.Add(new(reader.GetString(0), reader.GetString(1)));
        return Results.Ok(new { dependencias, periodos, unidadesMedida });
    }

    private static async Task<IResult> AuditoriaAsync(long? antesDe, SesionInstitucional sesion, CancellationToken ct)
    {
        if (antesDe is <= 0)
            return Results.Problem(statusCode: 400, title: "El cursor de auditoría debe ser un entero positivo.");
        const string sql = """
            SELECT id, institucion_id, emisor, sujeto, accion, entidad, referencia, fecha FROM sisplan.auditoria
            WHERE institucion_id = @institucion AND (@antes IS NULL OR id < @antes)
            ORDER BY id DESC LIMIT 100;
            """;
        await using var command = sesion.CreateCommand(sql);
        command.Parameters.AddWithValue("institucion", sesion.Activa!.Id);
        command.Parameters.AddWithValue("antes", NpgsqlDbType.Bigint, (object?)antesDe ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var eventos = new List<Evento>();
        while (await reader.ReadAsync(ct))
            eventos.Add(new(reader.GetInt64(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetDateTime(7)));
        return Results.Ok(eventos);
    }
}
