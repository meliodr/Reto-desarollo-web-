using System.Text.Json;
using Npgsql;

namespace Sisplan.Api;

internal static class CatalogoPeiEndpoints
{
    private sealed record EjeCatalogo(string Nombre, ObjetivoCatalogo[] Objetivos);
    private sealed record ObjetivoCatalogo(string Codigo, string Nombre, string Descripcion,
        string ResultadoEsperado, string Indicador, string LineaAccion);

    public static void MapCatalogoPei(this RouteGroupBuilder api)
    {
        api.MapPost("/peis/{peiId:guid}/catalogo-digemaps", ImportAsync);
    }

    private static async Task<IResult> ImportAsync(Guid peiId, NpgsqlDataSource source, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, ct);
        if (parent is null)
            return Results.NotFound();
        if (!PeiEstados.Editable(parent.Value.State))
            return Results.Problem(statusCode: 409, title: "El PEI no está disponible para cargar su estructura.");

        const string check = "SELECT EXISTS (SELECT 1 FROM sisplan.eje WHERE pei_version_id = @version)";
        await using var existing = new NpgsqlCommand(check, connection, transaction);
        existing.Parameters.AddWithValue("version", parent.Value.Id);
        if (await existing.ExecuteScalarAsync(ct) is true)
            return Results.Problem(statusCode: 409, title: "La carga de DIGEMAPS requiere un PEI sin ejes.");

        using var resource = typeof(CatalogoPeiEndpoints).Assembly
            .GetManifestResourceStream("Sisplan.Api.Catalogos.digemaps.json")!;
        var catalog = await JsonSerializer.DeserializeAsync<EjeCatalogo[]>(resource,
            new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? throw new InvalidOperationException("El catálogo DIGEMAPS no está disponible.");
        var count = 0;
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
                    INSERT INTO sisplan.indicador (id, objetivo_id, nombre) VALUES (@indicatorId, @id, @indicador);
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
                count++;
            }
        }

        await transaction.CommitAsync(ct);
        return Results.Json(new { Ejes = catalog.Length, Objetivos = count, Indicadores = count }, statusCode: 201);
    }
}
