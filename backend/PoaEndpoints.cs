using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace Sisplan.Api;

internal static class PoaEndpoints
{
    private sealed record Solicitud(int Anio, Guid? PeiId, string? PresupuestoReferencia, Guid? DependenciaId);
    private sealed record Poa(Guid Id, int Anio, string Estado, DateOnly FechaInicio, DateOnly FechaFin,
        Guid? PeiId, string? PeiNombre, string? PresupuestoReferencia, Guid? DependenciaId, Guid? PeiVersionId);

    public static void MapPoas(this RouteGroupBuilder api)
    {
        api.MapGet("/poas", async (SesionInstitucional source, CancellationToken ct) =>
        {
            const string sql = """
                SELECT a.id, a.anio, a.estado, a.fecha_inicio, a.fecha_fin, v.pei_id, v.nombre, a.presupuesto_referencia, a.dependencia_id, v.id
                FROM sisplan.poa a LEFT JOIN sisplan.pei_version v ON @mostrarPei AND v.id = a.pei_version_id
                ORDER BY a.anio DESC, a.id
                """;
            await using var command = source.CreateCommand(sql);
            command.Parameters.AddWithValue("mostrarPei", source.Activa!.Modulos.Pei);
            await using var reader = await command.ExecuteReaderAsync(ct);
            var plans = new List<Poa>();
            while (await reader.ReadAsync(ct))
            {
                plans.Add(ReadPoa(reader));
            }
            return Results.Ok(plans);
        });
        api.MapPost("/poas", CreateAsync).RequireAuthorization("FormularPoa");
    }

    private static async Task<IResult> CreateAsync(HttpRequest request, SesionInstitucional source, CancellationToken ct)
    {
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía el POA como JSON.");
        Solicitud? input;
        try { input = await request.ReadFromJsonAsync<Solicitud>(ct); }
        catch (JsonException) { return Results.Problem(statusCode: 400, title: "El contenido del POA no es válido."); }
        if (input is null || input.Anio is < 1 or > 9999)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["anio"] = ["Indica un año entre 1 y 9999."] }, statusCode: 422);

        var institucion = source.Activa!;
        if (input.PeiId.HasValue && !institucion.Modulos.Pei)
            return Results.Problem(statusCode: 403, title: "Vincular un PEI requiere que ambos módulos estén habilitados.");
        var dependencia = input.DependenciaId ?? (institucion.LimitadaAlArea ? institucion.DependenciaId : null);
        if (institucion.LimitadaAlArea && dependencia != institucion.DependenciaId)
            return DependenciaDenegada();

        decimal? presupuesto = null;
        if (input.PresupuestoReferencia is { } referencia)
        {
            if (!Regex.IsMatch(referencia, @"\A[0-9]{1,12}(\.[0-9]{1,2})?\z"))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["presupuestoReferencia"] = ["Indica un importe en pesos dominicanos con hasta 12 dígitos enteros y 2 decimales."]
                }, statusCode: 422);
            presupuesto = decimal.Parse(referencia, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }

        const string sql = """
            WITH area AS (
                SELECT id FROM sisplan.dependencia WHERE id = @dependencia AND institucion_id = @institucion AND activa
            ), plan AS (
                SELECT id, nombre, fecha_inicio, fecha_fin
                FROM sisplan.pei_version
                WHERE pei_id = @peiId AND estado = 'publicado'
                  AND daterange(fecha_inicio, fecha_fin, '[]') && daterange(make_date(@anio, 1, 1), make_date(@anio, 12, 31), '[]')
                ORDER BY numero DESC LIMIT 1
            ), inserted AS (
                INSERT INTO sisplan.poa(id, pei_id, pei_version_id, anio, fecha_inicio, fecha_fin, presupuesto_referencia, dependencia_id)
                SELECT @id, @peiId, plan.id, @anio,
                    greatest(make_date(@anio, 1, 1), plan.fecha_inicio),
                    least(make_date(@anio, 12, 31), plan.fecha_fin),
                    @presupuesto, @dependencia
                FROM (SELECT 1) AS seed LEFT JOIN plan ON true
                WHERE (@peiId IS NULL OR plan.id IS NOT NULL) AND (@dependencia IS NULL OR EXISTS (SELECT FROM area))
                ON CONFLICT DO NOTHING
                RETURNING id, anio, estado, fecha_inicio, fecha_fin, pei_id, presupuesto_referencia, dependencia_id, pei_version_id
            )
            SELECT i.id, i.anio, i.estado, i.fecha_inicio, i.fecha_fin, i.pei_id, p.nombre, i.presupuesto_referencia, i.dependencia_id, i.pei_version_id,
                @peiId IS NULL OR p.id IS NOT NULL AS vinculo_valido,
                @dependencia IS NULL OR EXISTS (SELECT FROM area) AS area_valida
            FROM (SELECT 1) AS seed LEFT JOIN inserted i ON true LEFT JOIN plan p ON true
            """;
        await using var command = source.CreateCommand(sql);
        command.Parameters.AddWithValue("institucion", institucion.Id);
        command.Parameters.AddWithValue("dependencia", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)dependencia ?? DBNull.Value);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("anio", input.Anio);
        command.Parameters.AddWithValue("peiId", NpgsqlTypes.NpgsqlDbType.Uuid, (object?)input.PeiId ?? DBNull.Value);
        command.Parameters.AddWithValue("presupuesto", NpgsqlTypes.NpgsqlDbType.Numeric, (object?)presupuesto ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        if (!reader.GetBoolean(11))
            return DependenciaDenegada();
        if (!reader.GetBoolean(10))
            return Results.Problem(statusCode: 409, title: "Elige un PEI publicado que cubra el año del POA.");
        if (reader.IsDBNull(0))
            return Results.Problem(statusCode: 409, title: "Ya existe un POA para ese año y esa modalidad.");
        return Results.Created("/api/v1/poas", ReadPoa(reader));
    }

    private static IResult DependenciaDenegada() =>
        Results.Problem(statusCode: 403, title: "La dependencia no está disponible en tu ámbito de trabajo.");

    private static Poa ReadPoa(NpgsqlDataReader reader) =>
        new(reader.GetGuid(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetFieldValue<DateOnly>(3), reader.GetFieldValue<DateOnly>(4),
            reader.IsDBNull(5) ? null : reader.GetGuid(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetDecimal(7).ToString("0.00", CultureInfo.InvariantCulture),
            reader.IsDBNull(8) ? null : reader.GetGuid(8),
            reader.IsDBNull(9) ? null : reader.GetGuid(9));
}
