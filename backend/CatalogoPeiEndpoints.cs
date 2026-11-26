using System.Text.Json;
using Npgsql;

namespace Sisplan.Api;

internal static class CatalogoPeiEndpoints
{
    private sealed record EjeCatalogo(string Nombre, ObjetivoCatalogo[] Objetivos);
    private static readonly JsonSerializerOptions CatalogJson = new(JsonSerializerDefaults.Web);
    private sealed record ObjetivoCatalogo(string Codigo, string Nombre, string Descripcion,
        string ResultadoEsperado, string Indicador, string LineaAccion);

    public static void MapCatalogoPei(this RouteGroupBuilder api)
    {
        api.MapPost("/peis/{peiId:guid}/catalogo-digemaps", ImportAsync).RequireAuthorization("EditarPei");
    }

    private static async Task<IResult> ImportAsync(Guid peiId, SesionInstitucional source, CancellationToken ct)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, ct);
        if (parent is null)
            return Results.NotFound();
        const string check = """
            SELECT v.catalogo_digemaps_cargado,
                   EXISTS (SELECT 1 FROM sisplan.eje e WHERE e.pei_version_id = v.id)
            FROM sisplan.pei_version v WHERE v.id = @version
            """;
        await using var existing = new NpgsqlCommand(check, connection, transaction);
        existing.Parameters.AddWithValue("version", parent.Value.Id);
        bool loaded;
        bool hasAxes;
        await using (var reader = await existing.ExecuteReaderAsync(ct))
        {
            await reader.ReadAsync(ct);
            loaded = reader.GetBoolean(0);
            hasAxes = reader.GetBoolean(1);
        }
        if (loaded)
            return Results.Ok(Summary(await ReadCatalogAsync(ct)));

        if (!PeiEstados.Editable(parent.Value.State))
            return Results.Problem(statusCode: 409, title: "El PEI no está disponible para cargar su estructura.");

        if (hasAxes)
            return Results.Problem(statusCode: 409, title: "La carga de DIGEMAPS requiere un PEI sin ejes.");

        var catalog = await ReadCatalogAsync(ct);
        foreach (var axis in catalog)
        {
            const string insertAxis = """
                INSERT INTO sisplan.eje (id, pei_version_id) VALUES (@id, @version);
                INSERT INTO sisplan.eje_version (eje_id, revision, nombre, activo) VALUES (@id, 1, @nombre, true);
                """;
            var axisId = Guid.NewGuid();
            await using var axisCommand = new NpgsqlCommand(insertAxis, connection, transaction);
            axisCommand.Parameters.AddWithValue("id", axisId);
            axisCommand.Parameters.AddWithValue("version", parent.Value.Id);
            axisCommand.Parameters.AddWithValue("nombre", axis.Nombre);
            await axisCommand.ExecuteNonQueryAsync(ct);
            foreach (var goal in axis.Objetivos)
            {
                const string insertGoal = """
                    INSERT INTO sisplan.objetivo (id, eje_id, eje_revision, codigo, nombre, descripcion, resultado_esperado, linea_accion)
                    VALUES (@id, @eje, 1, @codigo, @nombre, @descripcion, @resultado, @linea);
                    INSERT INTO sisplan.indicador (id, objetivo_id, nombre, origen_digemaps)
                    VALUES (@indicatorId, @id, @indicador, true);
                    """;
                await using var goalCommand = new NpgsqlCommand(insertGoal, connection, transaction);
                goalCommand.Parameters.AddWithValue("id", Guid.NewGuid());
                goalCommand.Parameters.AddWithValue("indicatorId", Guid.NewGuid());
                goalCommand.Parameters.AddWithValue("eje", axisId);
                goalCommand.Parameters.AddWithValue("codigo", goal.Codigo);
                goalCommand.Parameters.AddWithValue("nombre", goal.Nombre);
                goalCommand.Parameters.AddWithValue("descripcion", goal.Descripcion);
                goalCommand.Parameters.AddWithValue("resultado", goal.ResultadoEsperado);
                goalCommand.Parameters.AddWithValue("linea", goal.LineaAccion);
                goalCommand.Parameters.AddWithValue("indicador", goal.Indicador);
                await goalCommand.ExecuteNonQueryAsync(ct);
            }
        }

        await using var mark = new NpgsqlCommand("""
            UPDATE sisplan.pei_version SET catalogo_digemaps_cargado = true WHERE id = @version
            """, connection, transaction);
        mark.Parameters.AddWithValue("version", parent.Value.Id);
        await mark.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Json(Summary(catalog), statusCode: 201);
    }

    private static object Summary(EjeCatalogo[] catalog)
    {
        var goals = catalog.Sum(axis => axis.Objetivos.Length);
        return new { Ejes = catalog.Length, Objetivos = goals, Indicadores = goals };
    }

    private static async Task<EjeCatalogo[]> ReadCatalogAsync(CancellationToken ct)
    {
        using var resource = typeof(CatalogoPeiEndpoints).Assembly
            .GetManifestResourceStream("Sisplan.Api.Catalogos.digemaps.json")!;
        return await JsonSerializer.DeserializeAsync<EjeCatalogo[]>(resource, CatalogJson, ct)
            ?? throw new InvalidOperationException("El catálogo DIGEMAPS no está disponible.");
    }
}
