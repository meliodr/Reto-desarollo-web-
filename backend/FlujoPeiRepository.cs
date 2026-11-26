using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class FlujoPeiRepository(SesionInstitucional source, PeiRepository peis)
{
    private const string Fields = "pei_id, id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores, estado, revision";

    public async Task<FlujoPeiOpciones?> OptionsAsync(Guid versionId, FlujoPeiActor actor, CancellationToken ct)
    {
        var connection = source.Conexion;
        var snapshot = await FindCurrentAsync(versionId, connection, null, ct);
        if (snapshot is null)
            return null;

        var actions = await AllowedActionsAsync(snapshot.Pei.Estado, versionId, actor, connection, null, ct);
        string? returnReason = null;
        if (snapshot.Pei.Estado is "observado" or "corregido" or "borrador")
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
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var snapshot = await FindCurrentAsync(versionId, connection, transaction, ct);
        if (snapshot is null)
            return new(FlujoPeiStatus.NotFound);
        if (!string.Equals(expectedTag, snapshot.ETag, StringComparison.Ordinal))
            return new(FlujoPeiStatus.Stale);

        var state = snapshot.Pei.Estado;
        if (action == "publicar" && state == "publicado")
            return actor.Director ? new(FlujoPeiStatus.Saved, snapshot) : new(FlujoPeiStatus.Forbidden);

        if (!IsActionForState(state, action))
            return new(FlujoPeiStatus.Conflict);
        var allowed = await AllowedActionsAsync(state, versionId, actor, connection, transaction, ct);
        if (!allowed.Contains(action))
            return new(FlujoPeiStatus.Forbidden);

        var next = action switch
        {
            "enviar" => "en_revision",
            "devolver" => "observado",
            "corregir" => "corregido",
            "aprobar" => "aprobado",
            "publicar" => "publicado",
            _ => state,
        };

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
            LEFT JOIN sisplan.poa_evaluacion e ON e.poa_id = a.id
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
            if (!reader.IsDBNull(6))
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

    private static bool IsActionForState(string state, string action) => state switch
    {
        "borrador" or "corregido" => action == "enviar",
        "observado" => action == "corregir",
        "en_revision" => action is "revisar" or "validar_comite" or "aprobar" or "devolver",
        "aprobado" => action == "publicar",
        _ => false,
    };

    private static async Task<string[]> AllowedActionsAsync(string state, Guid versionId, FlujoPeiActor actor,
        NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken ct)
    {
        if (state is "borrador" or "corregido")
            return actor.Analista ? ["enviar"] : [];
        if (state == "observado")
            return actor.Analista ? ["corregir"] : [];
        if (state == "aprobado")
            return actor.Director ? ["publicar"] : [];
        if (state != "en_revision")
            return [];

        const string sql = """
            SELECT accion, emisor = @emisor AND sujeto = @sujeto
            FROM sisplan.pei_transicion
            WHERE pei_version_id = @id AND accion IN ('enviar', 'revisar', 'validar_comite')
            ORDER BY revision DESC
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", versionId);
        command.Parameters.AddWithValue("emisor", actor.Emisor);
        command.Parameters.AddWithValue("sujeto", actor.Sujeto);
        await using var reader = await command.ExecuteReaderAsync(ct);
        string? stage = null;
        while (await reader.ReadAsync(ct))
        {
            var step = reader.GetString(0);
            stage ??= step;
            if (reader.GetBoolean(1))
                return [];
            if (step == "enviar")
                break;
        }

        return stage switch
        {
            "enviar" when actor.Director => ["revisar", "devolver"],
            "revisar" when actor.Comite => ["validar_comite", "devolver"],
            "validar_comite" when actor.Autoridad => ["aprobar", "devolver"],
            _ => [],
        };
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
