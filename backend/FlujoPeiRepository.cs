using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class FlujoPeiRepository(NpgsqlDataSource source, PeiRepository peis)
{
    private const string Fields = "pei_id, id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores, estado, revision";

    public async Task<FlujoPeiOpciones?> OptionsAsync(Guid versionId, FlujoPeiActor actor, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        var snapshot = await FindCurrentAsync(versionId, connection, null, ct);
        if (snapshot is null)
            return null;

        var actions = snapshot.Pei.Estado switch
        {
            "borrador" => new[] { "enviar" },
            "en_revision" => await IsSubmitterAsync(versionId, actor, connection, null, ct)
                ? new[] { "devolver" } : new[] { "aprobar", "devolver" },
            "aprobado" => new[] { "publicar" },
            _ => Array.Empty<string>()
        };
        string? returnReason = null;
        if (snapshot.Pei.Estado == "borrador")
        {
            await using var command = new NpgsqlCommand("""
                SELECT motivo FROM sisplan.pei_transicion
                WHERE pei_version_id = @id AND accion = 'devolver'
                ORDER BY revision DESC LIMIT 1
                """, connection);
            command.Parameters.AddWithValue("id", versionId);
            returnReason = await command.ExecuteScalarAsync(ct) as string;
        }
        return new FlujoPeiOpciones(actions, returnReason);
    }

    public async Task<FlujoPeiResult> ApplyAsync(Guid versionId, string expectedTag, string action, string? reason,
        FlujoPeiActor actor, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var snapshot = await FindCurrentAsync(versionId, connection, transaction, ct);
        if (snapshot is null)
            return new(FlujoPeiStatus.NotFound);
        if (!string.Equals(expectedTag, snapshot.ETag, StringComparison.Ordinal))
            return new(FlujoPeiStatus.Stale);

        var state = snapshot.Pei.Estado;
        if (action == "publicar" && state == "publicado")
            return new(FlujoPeiStatus.Saved, snapshot);

        var next = (state, action) switch
        {
            ("borrador", "enviar") => "en_revision",
            ("en_revision", "aprobar") => "aprobado",
            ("en_revision", "devolver") => "borrador",
            ("aprobado", "publicar") => "publicado",
            _ => null
        };
        if (next is null)
            return new(FlujoPeiStatus.Conflict);
        if (action == "aprobar" && await IsSubmitterAsync(versionId, actor, connection, transaction, ct))
            return new(FlujoPeiStatus.SelfApproval);

        if (action is "aprobar" or "publicar")
        {
            if (!await HasCompleteStructureAsync(versionId, connection, transaction, ct))
                return new(FlujoPeiStatus.IncompleteStructure);
        }

        if (action == "publicar")
        {
            // El intervalo se comprueba bajo un bloqueo compartido por todas las publicaciones.
            await using (var publishLock = new NpgsqlCommand("SELECT pg_advisory_xact_lock(1856372216)", connection, transaction))
                await publishLock.ExecuteNonQueryAsync(ct);
            if (await OverlapsAsync(snapshot.Pei, connection, transaction, ct))
                return new(FlujoPeiStatus.OverlappingPeriod);
            await GeneratePoasAsync(snapshot.Pei, connection, transaction, ct);
        }

        const string updateSql = """
            UPDATE sisplan.pei_version
            SET estado = @estado, revision = revision + 1, actualizado_en = clock_timestamp()
            WHERE id = @id
            RETURNING revision
            """;
        await using var update = new NpgsqlCommand(updateSql, connection, transaction);
        update.Parameters.AddWithValue("estado", next);
        update.Parameters.AddWithValue("id", versionId);
        var revision = (long)(await update.ExecuteScalarAsync(ct)
            ?? throw new InvalidOperationException("No se actualizó el estado del PEI."));

        await RecordTransitionAsync(versionId, revision, action, state, next, actor, reason, connection, transaction, ct);
        await transaction.CommitAsync(ct);
        return new(FlujoPeiStatus.Saved, new PeiSnapshot(snapshot.Pei with { Estado = next }, revision));
    }

    public async Task<IReadOnlyList<FlujoPeiPoaDto>?> ListPoasAsync(Guid peiId, CancellationToken ct)
    {
        if (await peis.FindAsync(peiId, ct) is null)
            return null;

        const string sql = """
            SELECT a.id, a.anio, a.estado, a.fecha_inicio, a.fecha_fin,
                   v.estado = 'publicado' AND @hoy BETWEEN a.fecha_inicio AND a.fecha_fin,
                   e.id, e.cuatrimestre, e.fecha_inicio, e.fecha_fin, e.estado
            FROM sisplan.poa a
            JOIN sisplan.pei_version v ON v.id = a.pei_version_id
            JOIN sisplan.poa_evaluacion e ON e.poa_id = a.id
            WHERE a.pei_id = @peiId
            ORDER BY a.anio, e.cuatrimestre
            """;

        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        await using var command = source.CreateCommand(sql);
        command.Parameters.AddWithValue("peiId", peiId);
        command.Parameters.AddWithValue("hoy", NpgsqlDbType.Date, today);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var poas = new List<FlujoPeiPoaDto>();
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetGuid(0);
            if (poas.Count == 0 || poas[^1].Id != id)
                poas.Add(new FlujoPeiPoaDto(id, reader.GetInt32(1), reader.GetString(2),
                    reader.GetFieldValue<DateOnly>(3), reader.GetFieldValue<DateOnly>(4), reader.GetBoolean(5), []));
            poas[^1].Evaluaciones.Add(new FlujoPeiEvaluacionDto(reader.GetGuid(6), reader.GetInt32(7),
                reader.GetFieldValue<DateOnly>(8), reader.GetFieldValue<DateOnly>(9), reader.GetString(10)));
        }
        return poas;
    }

    private static async Task<PeiSnapshot?> FindCurrentAsync(Guid versionId, NpgsqlConnection connection,
        NpgsqlTransaction? transaction, CancellationToken ct)
    {
        // Los cambios de ejes, objetivos e indicadores toman primero el bloqueo del PEI.
        if (transaction is not null)
        {
            const string parentSql = """
                SELECT p.id FROM sisplan.pei p
                JOIN sisplan.pei_version v ON v.pei_id = p.id
                WHERE v.id = @versionId FOR UPDATE OF p
                """;
            await using var parent = new NpgsqlCommand(parentSql, connection, transaction);
            parent.Parameters.AddWithValue("versionId", versionId);
            if (await parent.ExecuteScalarAsync(ct) is null)
                return null;
        }

        var sql = $"""
            SELECT {Fields} FROM sisplan.pei_version v WHERE id = @versionId
            AND NOT EXISTS (SELECT 1 FROM sisplan.pei_version posterior
                WHERE posterior.pei_id = v.pei_id AND posterior.numero > v.numero)
            """;
        if (transaction is not null)
            sql += " FOR UPDATE";

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("versionId", versionId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new PeiSnapshot(new PeiDto(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                reader.GetFieldValue<DateOnly>(4), reader.GetFieldValue<DateOnly>(5), reader.GetString(6),
                reader.GetString(7), reader.GetString(8), reader.GetString(9)), reader.GetInt64(10))
            : null;
    }

    private static async Task<bool> IsSubmitterAsync(Guid versionId, FlujoPeiActor actor, NpgsqlConnection connection,
        NpgsqlTransaction? transaction, CancellationToken ct)
    {
        const string sql = """
            SELECT emisor = @emisor AND sujeto = @sujeto FROM sisplan.pei_transicion
            WHERE pei_version_id = @id AND accion = 'enviar' ORDER BY revision DESC LIMIT 1
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", versionId);
        command.Parameters.AddWithValue("emisor", actor.Emisor);
        command.Parameters.AddWithValue("sujeto", actor.Sujeto);
        // Una revisión sin un envío registrado tampoco habilita la aprobación.
        return await command.ExecuteScalarAsync(ct) is not false;
    }

    private static async Task<bool> HasCompleteStructureAsync(Guid versionId, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken ct)
    {
        const string sql = """
            WITH activos AS (
                SELECT e.id
                FROM sisplan.eje e
                JOIN LATERAL (
                    SELECT activo FROM sisplan.eje_version WHERE eje_id = e.id ORDER BY revision DESC LIMIT 1
                ) v ON v.activo
                WHERE e.pei_version_id = @id
            )
            SELECT EXISTS (SELECT 1 FROM activos)
                AND NOT EXISTS (
                    SELECT 1 FROM activos e
                    WHERE NOT EXISTS (SELECT 1 FROM sisplan.objetivo o WHERE o.eje_id = e.id)
                    OR EXISTS (
                        SELECT 1 FROM sisplan.objetivo o WHERE o.eje_id = e.id
                        AND NOT EXISTS (SELECT 1 FROM sisplan.indicador i WHERE i.objetivo_id = o.id)
                    )
                )
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", versionId);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    private static async Task<bool> OverlapsAsync(PeiDto pei, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken ct)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM sisplan.pei_version WHERE estado = 'publicado' AND pei_id <> @peiId
                AND fecha_inicio <= @fin AND fecha_fin >= @inicio
            )
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("peiId", pei.Id);
        command.Parameters.AddWithValue("inicio", NpgsqlDbType.Date, pei.FechaInicio);
        command.Parameters.AddWithValue("fin", NpgsqlDbType.Date, pei.FechaFin);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    private static async Task GeneratePoasAsync(PeiDto pei, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken ct)
    {
        const string sql = """
            WITH planes AS (
                INSERT INTO sisplan.poa (id, pei_id, pei_version_id, anio, fecha_inicio, fecha_fin)
                SELECT gen_random_uuid(), @peiId, @versionId, anio,
                    GREATEST(@inicio, make_date(anio, 1, 1)),
                    LEAST(@fin, make_date(anio, 12, 31))
                FROM generate_series(@anioInicio, @anioFin) anio
                ON CONFLICT (pei_id, anio) DO NOTHING
                RETURNING id, anio, fecha_inicio, fecha_fin
            )
            INSERT INTO sisplan.poa_evaluacion (id, poa_id, cuatrimestre, fecha_inicio, fecha_fin, estado)
            SELECT gen_random_uuid(), p.id, c.numero, fechas.inicio, fechas.fin,
                CASE WHEN fechas.fin < p.fecha_inicio OR fechas.inicio > p.fecha_fin
                     THEN 'fuera_de_vigencia' ELSE 'generada' END
            FROM planes p
            CROSS JOIN (VALUES (1, 1, 4, 30), (2, 5, 8, 31), (3, 9, 12, 31))
                AS c(numero, mes_inicio, mes_fin, dia_fin)
            CROSS JOIN LATERAL (
                SELECT make_date(p.anio, c.mes_inicio, 1) AS inicio,
                       make_date(p.anio, c.mes_fin, c.dia_fin) AS fin
            ) fechas
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("peiId", pei.Id);
        command.Parameters.AddWithValue("versionId", pei.VersionId);
        command.Parameters.AddWithValue("anioInicio", pei.FechaInicio.Year);
        command.Parameters.AddWithValue("anioFin", pei.FechaFin.Year);
        command.Parameters.AddWithValue("inicio", NpgsqlDbType.Date, pei.FechaInicio);
        command.Parameters.AddWithValue("fin", NpgsqlDbType.Date, pei.FechaFin);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task RecordTransitionAsync(Guid versionId, long revision, string action, string previous,
        string next, FlujoPeiActor actor, string? reason, NpgsqlConnection connection, NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO sisplan.pei_transicion
                (pei_version_id, revision, accion, estado_anterior, estado_nuevo, emisor, sujeto, motivo)
            VALUES (@id, @revision, @accion, @anterior, @nuevo, @emisor, @sujeto, @motivo)
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", versionId);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("accion", action);
        command.Parameters.AddWithValue("anterior", previous);
        command.Parameters.AddWithValue("nuevo", next);
        command.Parameters.AddWithValue("emisor", actor.Emisor);
        command.Parameters.AddWithValue("sujeto", actor.Sujeto);
        command.Parameters.AddWithValue("motivo", NpgsqlDbType.Text, (object?)reason ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
    }
}
