using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class EjeRepository(NpgsqlDataSource source, PeiRepository peis)
{
    private const string LatestEjes = """
        SELECT e.id, v.nombre, v.descripcion, v.activo, v.revision, v.actualizado_en
        FROM sisplan.eje e
        JOIN LATERAL (
            SELECT nombre, descripcion, activo, revision, actualizado_en
            FROM sisplan.eje_version WHERE eje_id = e.id ORDER BY revision DESC LIMIT 1
        ) v ON true
        WHERE e.pei_version_id = @peiVersionId
        """;

    public async Task<IReadOnlyList<EjeDto>?> ListAsync(Guid peiId, CancellationToken cancellationToken)
    {
        var pei = await peis.FindAsync(peiId, cancellationToken);
        if (pei is null)
            return null;

        await using var command = source.CreateCommand(LatestEjes + "\nORDER BY e.creado_en, e.id");
        command.Parameters.AddWithValue("peiVersionId", pei.Pei.VersionId);
        return await ReadListAsync(command, cancellationToken);
    }

    public async Task<EjeDto?> FindAsync(Guid peiId, Guid ejeId, CancellationToken cancellationToken)
    {
        var pei = await peis.FindAsync(peiId, cancellationToken);
        if (pei is null)
            return null;

        await using var command = source.CreateCommand(LatestEjes + "\nAND e.id = @ejeId");
        command.Parameters.AddWithValue("peiVersionId", pei.Pei.VersionId);
        command.Parameters.AddWithValue("ejeId", ejeId);
        return (await ReadListAsync(command, cancellationToken)).SingleOrDefault();
    }

    public async Task<IReadOnlyList<EjeDto>?> HistoryAsync(Guid peiId, Guid ejeId, CancellationToken cancellationToken)
    {
        var pei = await peis.FindAsync(peiId, cancellationToken);
        if (pei is null)
            return null;

        const string sql = """
            SELECT e.id, v.nombre, v.descripcion, v.activo, v.revision, v.actualizado_en
            FROM sisplan.eje e
            JOIN sisplan.eje_version v ON v.eje_id = e.id
            WHERE e.pei_version_id = @peiVersionId AND e.id = @ejeId
            ORDER BY v.revision DESC
            """;
        await using var command = source.CreateCommand(sql);
        command.Parameters.AddWithValue("peiVersionId", pei.Pei.VersionId);
        command.Parameters.AddWithValue("ejeId", ejeId);
        var versions = await ReadListAsync(command, cancellationToken);
        return versions.Count > 0 ? versions : null;
    }

    public async Task<EjeWriteResult> CreateAsync(Guid peiId, EjeFields fields, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockCurrentPeiVersionAsync(peiId, connection, transaction, cancellationToken);
        if (parent is null)
            return new(EjeWriteStatus.NotFound);
        if (parent.Value.State != "borrador")
            return new(EjeWriteStatus.Conflict);

        var ejeId = Guid.NewGuid();
        const string sql = "INSERT INTO sisplan.eje (id, pei_version_id) VALUES (@id, @versionId)";
        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", ejeId);
            command.Parameters.AddWithValue("versionId", parent.Value.Id);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var saved = await InsertVersionAsync(ejeId, 1, fields, true, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(EjeWriteStatus.Saved, saved);
    }

    public async Task<EjeWriteResult> UpdateAsync(Guid peiId, Guid ejeId, long revision, EjeFields fields,
        bool active, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockCurrentPeiVersionAsync(peiId, connection, transaction, cancellationToken);
        if (parent is null)
            return new(EjeWriteStatus.NotFound);

        var current = await FindCurrentAsync(parent.Value.Id, ejeId, connection, transaction, cancellationToken);
        if (current is null)
            return new(EjeWriteStatus.NotFound);
        if (parent.Value.State != "borrador")
            return new(EjeWriteStatus.Conflict);
        if (current.Revision != revision)
            return new(EjeWriteStatus.Stale);

        if (current.Nombre == fields.Nombre && current.Descripcion == fields.Descripcion && current.Activo == active)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(EjeWriteStatus.Saved, current);
        }

        var saved = await InsertVersionAsync(ejeId, revision + 1, fields, active, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(EjeWriteStatus.Saved, saved);
    }

    internal static async Task<EjeDto?> FindCurrentAsync(Guid peiVersionId, Guid ejeId,
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var lookup = new NpgsqlCommand(LatestEjes + "\nAND e.id = @ejeId", connection, transaction);
        lookup.Parameters.AddWithValue("peiVersionId", peiVersionId);
        lookup.Parameters.AddWithValue("ejeId", ejeId);
        return (await ReadListAsync(lookup, cancellationToken)).SingleOrDefault();
    }

    internal static async Task<(Guid Id, string State)?> LockCurrentPeiVersionAsync(Guid peiId,
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        // El bloqueo del PEI serializa los cambios de estructura, incluso entre ejes distintos.
        await using var plan = new NpgsqlCommand("SELECT id FROM sisplan.pei WHERE id = @id FOR UPDATE", connection, transaction);
        plan.Parameters.AddWithValue("id", peiId);
        if (await plan.ExecuteScalarAsync(cancellationToken) is null)
            return null;

        const string sql = """
            SELECT id, estado FROM sisplan.pei_version
            WHERE pei_id = @id ORDER BY numero DESC LIMIT 1 FOR UPDATE
            """;
        await using var version = new NpgsqlCommand(sql, connection, transaction);
        version.Parameters.AddWithValue("id", peiId);
        await using var reader = await version.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? (reader.GetGuid(0), reader.GetString(1)) : null;
    }

    private static async Task<EjeDto> InsertVersionAsync(Guid ejeId, long revision, EjeFields fields, bool active,
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sisplan.eje_version (eje_id, revision, nombre, descripcion, activo)
            VALUES (@id, @revision, @nombre, @descripcion, @activo)
            RETURNING eje_id, nombre, descripcion, activo, revision, actualizado_en
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", ejeId);
        command.Parameters.AddWithValue("revision", revision);
        command.Parameters.AddWithValue("nombre", fields.Nombre);
        command.Parameters.AddWithValue("descripcion", NpgsqlDbType.Text, (object?)fields.Descripcion ?? DBNull.Value);
        command.Parameters.AddWithValue("activo", active);
        return (await ReadListAsync(command, cancellationToken)).Single();
    }

    private static async Task<IReadOnlyList<EjeDto>> ReadListAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<EjeDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new EjeDto(reader.GetGuid(0), reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetBoolean(3),
                reader.GetInt64(4), reader.GetDateTime(5)));
        }
        return result;
    }
}
