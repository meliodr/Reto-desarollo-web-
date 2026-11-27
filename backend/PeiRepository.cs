using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class PeiRepository(SesionInstitucional source)
{
    internal const string Fields = "pei_id, id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores, estado, revision, version_origen_id";
    private const string SelectHead = """
        SELECT v.pei_id, v.id, v.nombre, v.tipo, v.fecha_inicio, v.fecha_fin,
               v.mision, v.vision, v.valores, v.estado, v.revision, v.version_origen_id
        FROM sisplan.pei p
        JOIN LATERAL (
            SELECT * FROM sisplan.pei_version WHERE pei_id = p.id AND (NOT @oficiales OR estado = 'publicado')
            ORDER BY numero DESC LIMIT 1
        ) v ON true
        """;

    public async Task<IReadOnlyList<PeiDto>> ListAsync(bool oficiales, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand(SelectHead + "\nORDER BY p.creado_en DESC, p.id DESC");
        command.Parameters.AddWithValue("oficiales", oficiales);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<PeiDto>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Read(reader).Pei);
        return result;
    }

    public async Task<PeiSnapshot?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand(SelectHead + "\nWHERE p.id = @id");
        command.Parameters.AddWithValue("oficiales", false);
        command.Parameters.AddWithValue("id", id);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<PeiSnapshot?> FindVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand($"SELECT {Fields} FROM sisplan.pei_version WHERE id = @id");
        command.Parameters.AddWithValue("id", versionId);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<PeiVersionDto>?> ListVersionsAsync(Guid peiId, DateOnly? fecha,
        CancellationToken ct)
    {
        if (await FindAsync(peiId, ct) is null)
            return null;

        const string sql = """
            SELECT v.id, v.numero, v.revision, v.estado, v.fecha_inicio, v.fecha_fin, v.actualizado_en,
                   publicada.registrada_en, v.version_origen_id, v.motivo_revision,
                   v.creada_por_emisor, v.creada_por_sujeto, v.creado_en
            FROM sisplan.pei_version v
            LEFT JOIN LATERAL (
                SELECT t.registrada_en FROM sisplan.pei_transicion t
                WHERE t.pei_version_id = v.id AND t.accion = 'publicar'
                ORDER BY t.revision DESC LIMIT 1
            ) publicada ON true
            WHERE v.pei_id = @peiId AND (@fecha IS NULL OR
                (v.estado = 'publicado' AND @fecha BETWEEN v.fecha_inicio AND v.fecha_fin))
            ORDER BY v.numero DESC
            """;
        await using var command = source.CreateCommand(sql);
        command.Parameters.AddWithValue("peiId", peiId);
        command.Parameters.AddWithValue("fecha", NpgsqlDbType.Date, (object?)fecha ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var versions = new List<PeiVersionDto>();
        while (await reader.ReadAsync(ct))
            versions.Add(new PeiVersionDto(reader.GetGuid(0), reader.GetInt32(1), reader.GetInt64(2), reader.GetString(3),
                reader.GetFieldValue<DateOnly>(4), reader.GetFieldValue<DateOnly>(5), reader.GetDateTime(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.IsDBNull(8) ? null : reader.GetGuid(8),
                reader.IsDBNull(9) ? null : reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11), reader.GetDateTime(12)));
        return versions;
    }

    public async Task<PeiSnapshot> CreateAsync(PeiFields fields, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var parent = new NpgsqlCommand("INSERT INTO sisplan.pei (id) VALUES (@id)", connection, transaction))
        {
            parent.Parameters.AddWithValue("id", id);
            await parent.ExecuteNonQueryAsync(cancellationToken);
        }

        var sql = $"""
            INSERT INTO sisplan.pei_version (id, pei_id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores)
            VALUES (@versionId, @peiId, @nombre, @tipo, @fechaInicio, @fechaFin, @mision, @vision, @valores)
            RETURNING {Fields}
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("versionId", versionId);
        command.Parameters.AddWithValue("peiId", id);
        AddFields(command, fields);
        var saved = await ReadOneAsync(command, cancellationToken)
            ?? throw new InvalidOperationException("El borrador no se devolvió al crearlo.");
        await transaction.CommitAsync(cancellationToken);
        return saved;
    }

    public async Task<PeiRevisionResult> CreateRevisionAsync(Guid peiId, string expectedTag, string reason,
        CancellationToken ct)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var parent = new NpgsqlCommand("SELECT id FROM sisplan.pei WHERE id = @id FOR UPDATE", connection, transaction))
        {
            parent.Parameters.AddWithValue("id", peiId);
            if (await parent.ExecuteScalarAsync(ct) is null)
                return new(PeiRevisionStatus.NotFound);
        }

        await using var lookup = new NpgsqlCommand(SelectHead + "\nWHERE p.id = @id", connection, transaction);
        lookup.Parameters.AddWithValue("id", peiId);
        lookup.Parameters.AddWithValue("oficiales", false);
        var current = await ReadOneAsync(lookup, ct);
        if (current is null)
            return new(PeiRevisionStatus.NotFound);
        if (current.Pei.Estado != "publicado")
            return new(PeiRevisionStatus.NotPublished);
        if (!string.Equals(current.ETag, expectedTag, StringComparison.Ordinal))
            return new(PeiRevisionStatus.Stale);

        var versionId = Guid.NewGuid();
        var sql = $"""
            INSERT INTO sisplan.pei_version
                (id, pei_id, numero, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores,
                 catalogo_digemaps_cargado, version_origen_id, motivo_revision, creada_por_emisor, creada_por_sujeto)
            SELECT @id, pei_id, numero + 1, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores,
                   catalogo_digemaps_cargado, id, @motivo, @emisor, @sujeto
            FROM sisplan.pei_version WHERE id = @origen
            RETURNING {Fields}
            """;
        await using var create = new NpgsqlCommand(sql, connection, transaction);
        create.Parameters.AddWithValue("id", versionId);
        create.Parameters.AddWithValue("origen", current.Pei.VersionId);
        create.Parameters.AddWithValue("motivo", reason);
        create.Parameters.AddWithValue("emisor", source.Emisor);
        create.Parameters.AddWithValue("sujeto", source.Sujeto);
        var saved = await ReadOneAsync(create, ct)
            ?? throw new InvalidOperationException("La revisión no se devolvió al crearla.");

        await CopyStructureAsync(current.Pei.VersionId, versionId, connection, transaction, ct);
        await transaction.CommitAsync(ct);
        return new(PeiRevisionStatus.Saved, saved);
    }

    private static async Task CopyStructureAsync(Guid original, Guid version, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken ct)
    {
        // La revisión parte del mapa vigente. Las mediciones pertenecen a los indicadores de su versión original.
        const string sql = """
            WITH mapa_ejes AS (
                SELECT id AS anterior, gen_random_uuid() AS nuevo
                FROM sisplan.eje WHERE pei_version_id = @original ORDER BY creado_en, id
            ), ejes AS (
                INSERT INTO sisplan.eje (id, pei_version_id)
                SELECT nuevo, @version FROM mapa_ejes RETURNING id
            ), eje_versiones AS (
                INSERT INTO sisplan.eje_version (eje_id, revision, nombre, descripcion, activo)
                SELECT e.id, 1, v.nombre, v.descripcion, v.activo
                FROM mapa_ejes m JOIN ejes e ON e.id = m.nuevo
                CROSS JOIN LATERAL (
                    SELECT nombre, descripcion, activo FROM sisplan.eje_version
                    WHERE eje_id = m.anterior ORDER BY revision DESC LIMIT 1
                ) v
                RETURNING eje_id
            ), mapa_objetivos AS (
                SELECT o.id AS anterior, gen_random_uuid() AS nuevo, e.eje_id, o.codigo, o.nombre,
                       o.descripcion, o.resultado_esperado, o.linea_accion, o.activo
                FROM mapa_ejes m JOIN eje_versiones e ON e.eje_id = m.nuevo
                JOIN sisplan.objetivo o ON o.eje_id = m.anterior
            ), objetivos AS (
                INSERT INTO sisplan.objetivo
                    (id, eje_id, eje_revision, codigo, nombre, descripcion, resultado_esperado, linea_accion, activo)
                SELECT nuevo, eje_id, 1, codigo, nombre, descripcion, resultado_esperado, linea_accion, activo
                FROM mapa_objetivos RETURNING id
            ), indicadores AS (
                INSERT INTO sisplan.indicador
                    (id, objetivo_id, nombre, formula, unidad, linea_base, meta, frecuencia, sentido, metodo_calculo, origen_digemaps)
                SELECT gen_random_uuid(), o.id, i.nombre, i.formula, i.unidad, i.linea_base, i.meta,
                       i.frecuencia, i.sentido, i.metodo_calculo, i.origen_digemaps
                FROM mapa_objetivos m JOIN objetivos o ON o.id = m.nuevo
                JOIN sisplan.indicador i ON i.objetivo_id = m.anterior
            )
            INSERT INTO sisplan.riesgo_estrategico
                (id, objetivo_id, nombre, descripcion, probabilidad, impacto, tratamiento, accion_mitigacion,
                 responsable, fecha_revision, evidencia_url, cerrado)
            SELECT gen_random_uuid(), o.id, r.nombre, r.descripcion, r.probabilidad, r.impacto, r.tratamiento,
                   r.accion_mitigacion, r.responsable, r.fecha_revision, r.evidencia_url, r.cerrado
            FROM mapa_objetivos m JOIN objetivos o ON o.id = m.nuevo
            JOIN sisplan.riesgo_estrategico r ON r.objetivo_id = m.anterior
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("original", original);
        command.Parameters.AddWithValue("version", version);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<PeiSnapshot?> UpdateAsync(Guid versionId, long expectedRevision, PeiFields fields, CancellationToken cancellationToken)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await LockParentAsync(versionId, connection, transaction, cancellationToken))
            return null;
        await using (var locked = new NpgsqlCommand("""
            SELECT v.revision FROM sisplan.pei_version v
            WHERE v.id = @id AND v.estado IN ('borrador', 'observado')
              AND NOT EXISTS (SELECT 1 FROM sisplan.pei_version posterior
                  WHERE posterior.pei_id = v.pei_id AND posterior.numero > v.numero)
            FOR UPDATE OF v
            """, connection, transaction))
        {
            locked.Parameters.AddWithValue("id", versionId);
            if (await locked.ExecuteScalarAsync(cancellationToken) is not long revision || revision != expectedRevision)
                return null;
        }

        await using (var measurements = new NpgsqlCommand("""
            SELECT min(m.periodo), max(m.periodo)
                FROM sisplan.indicador_medicion m
                JOIN sisplan.indicador i ON i.id = m.indicador_id
                JOIN sisplan.objetivo o ON o.id = i.objetivo_id
                JOIN sisplan.eje e ON e.id = o.eje_id
                WHERE e.pei_version_id = @id AND m.periodo NOT BETWEEN @inicio AND @fin
            """, connection, transaction))
        {
            measurements.Parameters.AddWithValue("id", versionId);
            measurements.Parameters.AddWithValue("inicio", NpgsqlDbType.Date, fields.FechaInicio);
            measurements.Parameters.AddWithValue("fin", NpgsqlDbType.Date, fields.FechaFin);
            await using var reader = await measurements.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken) && !reader.IsDBNull(0))
                throw new PeriodoConMedicionesException(
                    reader.GetFieldValue<DateOnly>(0) < fields.FechaInicio,
                    reader.GetFieldValue<DateOnly>(1) > fields.FechaFin);
        }

        var sql = $"""
            UPDATE sisplan.pei_version
            SET nombre = @nombre, tipo = @tipo, fecha_inicio = @fechaInicio, fecha_fin = @fechaFin,
                mision = @mision, vision = @vision, valores = @valores,
                revision = revision + 1, actualizado_en = clock_timestamp()
            WHERE id = @id AND revision = @revision AND estado IN ('borrador', 'observado')
            RETURNING {Fields}
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", versionId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        AddFields(command, fields);
        var saved = await ReadOneAsync(command, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return saved;
    }

    internal static async Task<bool> LockParentAsync(Guid versionId, NpgsqlConnection connection,
        NpgsqlTransaction transaction, CancellationToken ct)
    {
        // La edición y el flujo respetan el mismo orden de bloqueo que los cambios de estructura.
        const string sql = """
            SELECT p.id FROM sisplan.pei p JOIN sisplan.pei_version v ON v.pei_id = p.id
            WHERE v.id = @id FOR UPDATE OF p
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", versionId);
        return await command.ExecuteScalarAsync(ct) is Guid;
    }

    private static void AddFields(NpgsqlCommand command, PeiFields fields)
    {
        command.Parameters.AddWithValue("nombre", fields.Nombre);
        command.Parameters.AddWithValue("tipo", fields.Tipo);
        command.Parameters.AddWithValue("fechaInicio", NpgsqlDbType.Date, fields.FechaInicio);
        command.Parameters.AddWithValue("fechaFin", NpgsqlDbType.Date, fields.FechaFin);
        command.Parameters.AddWithValue("mision", fields.Mision);
        command.Parameters.AddWithValue("vision", fields.Vision);
        command.Parameters.AddWithValue("valores", fields.Valores);
    }

    private static async Task<PeiSnapshot?> ReadOneAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    internal static PeiSnapshot Read(NpgsqlDataReader reader) => new(
        new PeiDto(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
            reader.GetFieldValue<DateOnly>(4), reader.GetFieldValue<DateOnly>(5), reader.GetString(6),
            reader.GetString(7), reader.GetString(8), reader.GetString(9), reader.IsDBNull(11) ? null : reader.GetGuid(11)),
        reader.GetInt64(10));
}

internal sealed class PeriodoConMedicionesException(bool inicio, bool fin) : Exception
{
    public bool Inicio { get; } = inicio;
    public bool Fin { get; } = fin;
}
