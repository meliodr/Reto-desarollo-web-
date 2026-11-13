using Npgsql;

namespace Sisplan.Api;

internal sealed class ObjetivoRepository(NpgsqlDataSource source, EjeRepository ejes)
{
    private const string Fields = "id, codigo, nombre, descripcion, resultado_esperado, linea_accion";

    public async Task<IReadOnlyList<ObjetivoDto>?> ListAsync(Guid peiId, Guid ejeId, CancellationToken cancellationToken)
    {
        if (await ejes.FindAsync(peiId, ejeId, cancellationToken) is null)
            return null;

        await using var command = source.CreateCommand($"SELECT {Fields} FROM sisplan.objetivo WHERE eje_id = @ejeId ORDER BY codigo, id");
        command.Parameters.AddWithValue("ejeId", ejeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<ObjetivoDto>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Read(reader));
        return result;
    }

    public async Task<ObjetivoDto?> FindAsync(Guid peiId, Guid ejeId, Guid id, CancellationToken cancellationToken)
    {
        if (await ejes.FindAsync(peiId, ejeId, cancellationToken) is null)
            return null;

        await using var command = source.CreateCommand($"SELECT {Fields} FROM sisplan.objetivo WHERE eje_id = @ejeId AND id = @id");
        command.Parameters.AddWithValue("ejeId", ejeId);
        command.Parameters.AddWithValue("id", id);
        return await ReadOneAsync(command, cancellationToken);
    }

    public async Task<ObjetivoCreateResult> CreateAsync(Guid peiId, Guid ejeId, ObjetivoDto fields, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, cancellationToken);
        if (parent is null)
            return new(ObjetivoCreateStatus.NotFound);

        var eje = await EjeRepository.FindCurrentAsync(parent.Value.Id, ejeId, connection, transaction, cancellationToken);
        if (eje is null)
            return new(ObjetivoCreateStatus.NotFound);
        if (parent.Value.State != "borrador")
            return new(ObjetivoCreateStatus.ReadOnlyPei);
        if (!eje.Activo)
            return new(ObjetivoCreateStatus.InactiveAxis);

        var sql = $"""
            INSERT INTO sisplan.objetivo (id, eje_id, eje_revision, codigo, nombre, descripcion, resultado_esperado, linea_accion)
            VALUES (@id, @ejeId, @revision, @codigo, @nombre, @descripcion, @resultadoEsperado, @lineaAccion)
            ON CONFLICT ON CONSTRAINT objetivo_codigo_por_eje DO NOTHING
            RETURNING {Fields}
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", fields.Id);
        command.Parameters.AddWithValue("ejeId", ejeId);
        command.Parameters.AddWithValue("revision", eje.Revision);
        command.Parameters.AddWithValue("codigo", fields.Codigo);
        command.Parameters.AddWithValue("nombre", fields.Nombre);
        command.Parameters.AddWithValue("descripcion", fields.Descripcion);
        command.Parameters.AddWithValue("resultadoEsperado", fields.ResultadoEsperado);
        command.Parameters.AddWithValue("lineaAccion", fields.LineaAccion);

        var saved = await ReadOneAsync(command, cancellationToken);
        if (saved is null)
            return new(ObjetivoCreateStatus.DuplicateCode);

        await transaction.CommitAsync(cancellationToken);
        return new(ObjetivoCreateStatus.Saved, saved);
    }

    private static async Task<ObjetivoDto?> ReadOneAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    private static ObjetivoDto Read(NpgsqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5));
}
