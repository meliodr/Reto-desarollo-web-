using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class PeiRepository(NpgsqlDataSource source)
{
    private const string Fields = "pei_id, id, nombre, tipo, fecha_inicio, fecha_fin, mision, vision, valores, estado, revision";
    private const string SelectCurrent = """
        SELECT v.pei_id, v.id, v.nombre, v.tipo, v.fecha_inicio, v.fecha_fin,
               v.mision, v.vision, v.valores, v.estado, v.revision
        FROM sisplan.pei p
        JOIN LATERAL (
            SELECT * FROM sisplan.pei_version WHERE pei_id = p.id ORDER BY numero DESC LIMIT 1
        ) v ON true
        """;

    public async Task<IReadOnlyList<PeiDto>> ListAsync(CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand(SelectCurrent + "\nORDER BY p.creado_en DESC, p.id DESC");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<PeiDto>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Read(reader).Pei);
        return result;
    }

    public async Task<PeiSnapshot?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand(SelectCurrent + "\nWHERE p.id = @id");
        command.Parameters.AddWithValue("id", id);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<PeiSnapshot?> FindVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand($"SELECT {Fields} FROM sisplan.pei_version WHERE id = @id");
        command.Parameters.AddWithValue("id", versionId);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<PeiSnapshot> CreateAsync(PeiFields fields, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
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

    public async Task<PeiSnapshot?> UpdateAsync(Guid versionId, long expectedRevision, PeiFields fields, CancellationToken cancellationToken)
    {
        var sql = $"""
            UPDATE sisplan.pei_version
            SET nombre = @nombre, tipo = @tipo, fecha_inicio = @fechaInicio, fecha_fin = @fechaFin,
                mision = @mision, vision = @vision, valores = @valores,
                revision = revision + 1, actualizado_en = clock_timestamp()
            WHERE id = @id AND revision = @revision AND estado = 'borrador'
            RETURNING {Fields}
            """;
        await using var command = source.CreateCommand(sql);
        command.Parameters.AddWithValue("id", versionId);
        command.Parameters.AddWithValue("revision", expectedRevision);
        AddFields(command, fields);
        return await ReadOneAsync(command, cancellationToken);
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

    private static PeiSnapshot Read(NpgsqlDataReader reader) => new(
        new PeiDto(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
            reader.GetFieldValue<DateOnly>(4), reader.GetFieldValue<DateOnly>(5), reader.GetString(6),
            reader.GetString(7), reader.GetString(8), reader.GetString(9)),
        reader.GetInt64(10));
}
