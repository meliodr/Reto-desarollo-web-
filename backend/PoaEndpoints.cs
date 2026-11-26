using System.Text.Json;
using Npgsql;

namespace Sisplan.Api;

internal static class PoaEndpoints
{
    private sealed record Solicitud(int Anio, Guid? PeiId);
    private sealed record Poa(Guid Id, int Anio, string Estado, DateOnly FechaInicio, DateOnly FechaFin, Guid? PeiId, string? PeiNombre);

    public static void MapPoas(this RouteGroupBuilder api, bool authenticated)
    {
        api.MapGet("/poas", async (NpgsqlDataSource source, CancellationToken ct) =>
        {
            const string sql = """
                SELECT a.id, a.anio, a.estado, a.fecha_inicio, a.fecha_fin, a.pei_id, v.nombre
                FROM sisplan.poa a LEFT JOIN sisplan.pei_version v ON v.id = a.pei_version_id
                ORDER BY a.anio DESC, a.id
                """;
            await using var command = source.CreateCommand(sql);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var plans = new List<Poa>();
            while (await reader.ReadAsync(ct))
            {
                plans.Add(new(reader.GetGuid(0), reader.GetInt32(1), reader.GetString(2),
                    reader.GetFieldValue<DateOnly>(3), reader.GetFieldValue<DateOnly>(4),
                    reader.IsDBNull(5) ? null : reader.GetGuid(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)));
            }
            return Results.Ok(plans);
        });
        var create = api.MapPost("/poas", CreateAsync);
        if (authenticated)
            create.RequireAuthorization("FormularPoa");
    }

    private static async Task<IResult> CreateAsync(HttpRequest request, NpgsqlDataSource source, CancellationToken ct)
    {
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía el POA como JSON.");
        Solicitud? input;
        try { input = await request.ReadFromJsonAsync<Solicitud>(ct); }
        catch (JsonException) { return Results.Problem(statusCode: 400, title: "El contenido del POA no es válido."); }
        if (input is null || input.Anio is < 1 or > 9999)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["anio"] = ["Indica un año entre 1 y 9999."] }, statusCode: 422);

        const string sql = """
            WITH plan AS (
                SELECT id, nombre, fecha_inicio, fecha_fin
                FROM sisplan.pei_version
                WHERE pei_id = @peiId AND estado = 'publicado'
                  AND daterange(fecha_inicio, fecha_fin, '[]') && daterange(make_date(@anio, 1, 1), make_date(@anio, 12, 31), '[]')
                ORDER BY numero DESC LIMIT 1
            ), inserted AS (
                INSERT INTO sisplan.poa(id, pei_id, pei_version_id, anio, fecha_inicio, fecha_fin)
                SELECT @id, @peiId, plan.id, @anio,
                    greatest(make_date(@anio, 1, 1), plan.fecha_inicio),
                    least(make_date(@anio, 12, 31), plan.fecha_fin)
                FROM (SELECT 1) AS seed LEFT JOIN plan ON true
                WHERE @peiId IS NULL OR plan.id IS NOT NULL
                ON CONFLICT DO NOTHING
                RETURNING id, anio, estado, fecha_inicio, fecha_fin, pei_id
            )
            SELECT i.id, i.anio, i.estado, i.fecha_inicio, i.fecha_fin, i.pei_id, p.nombre,
                @peiId IS NULL OR p.id IS NOT NULL AS vinculo_valido
            FROM (SELECT 1) AS seed LEFT JOIN inserted i ON true LEFT JOIN plan p ON true
            """;
        await using var command = source.CreateCommand(sql);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("anio", input.Anio);
        command.Parameters.AddWithValue("peiId", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)input.PeiId ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        if (!reader.GetBoolean(7))
            return Results.Problem(statusCode: 409, title: "Elige un PEI publicado que cubra el año del POA.");
        if (reader.IsDBNull(0))
            return Results.Problem(statusCode: 409, title: "Ya existe un POA para ese año y esa modalidad.");
        var plan = new Poa(reader.GetGuid(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetFieldValue<DateOnly>(3), reader.GetFieldValue<DateOnly>(4), input.PeiId,
            reader.IsDBNull(6) ? null : reader.GetString(6));
        return Results.Created("/api/v1/poas", plan);
    }
}
