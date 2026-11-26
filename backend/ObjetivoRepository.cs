using Npgsql;

namespace Sisplan.Api;

internal sealed class ObjetivoRepository(SesionInstitucional source, EjeRepository ejes)
{
    internal const string Fields = "id, codigo, nombre, descripcion, resultado_esperado, linea_accion, activo, revision";

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
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, cancellationToken);
        if (parent is null)
            return new(ObjetivoCreateStatus.NotFound);

        var eje = await EjeRepository.FindCurrentAsync(parent.Value.Id, ejeId, connection, transaction, cancellationToken);
        if (eje is null)
            return new(ObjetivoCreateStatus.NotFound);
        if (!PeiEstados.Editable(parent.Value.State))
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

    public async Task<ObjetivoEstadoResult> SetActiveAsync(Guid peiId, Guid ejeId, Guid objetivoId, string expectedTag,
        bool active, CancellationToken ct)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, ct);
        if (parent is null)
            return new(ObjetivoEstadoStatus.NotFound);
        var axis = await EjeRepository.FindCurrentAsync(parent.Value.Id, ejeId, connection, transaction, ct);
        if (axis is null)
            return new(ObjetivoEstadoStatus.NotFound);

        await using var find = new NpgsqlCommand($"SELECT {Fields} FROM sisplan.objetivo WHERE eje_id=@eje AND id=@id",
            connection, transaction);
        find.Parameters.AddWithValue("eje", ejeId);
        find.Parameters.AddWithValue("id", objetivoId);
        var current = await ReadOneAsync(find, ct);
        if (current is null)
            return new(ObjetivoEstadoStatus.NotFound);
        if (expectedTag != current.ETag())
            return new(ObjetivoEstadoStatus.Stale);
        if (!PeiEstados.Editable(parent.Value.State))
            return new(ObjetivoEstadoStatus.ReadOnlyPei);
        if (!axis.Activo && active)
            return new(ObjetivoEstadoStatus.InactiveAxis);
        if (current.Activo == active)
            return new(ObjetivoEstadoStatus.Saved, current);

        await using var update = new NpgsqlCommand($"""
            UPDATE sisplan.objetivo SET activo=@activo, revision=revision+1
            WHERE eje_id=@eje AND id=@id RETURNING {Fields}
            """, connection, transaction);
        update.Parameters.AddWithValue("activo", active);
        update.Parameters.AddWithValue("eje", ejeId);
        update.Parameters.AddWithValue("id", objetivoId);
        var saved = await ReadOneAsync(update, ct);
        if (saved is null)
            return new(ObjetivoEstadoStatus.NotFound);
        await transaction.CommitAsync(ct);
        return new(ObjetivoEstadoStatus.Saved, saved);
    }

    private static async Task<ObjetivoDto?> ReadOneAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    internal static ObjetivoDto Read(NpgsqlDataReader reader, int offset = 0) => new(
        reader.GetGuid(offset), reader.GetString(offset + 1), reader.GetString(offset + 2),
        reader.GetString(offset + 3), reader.GetString(offset + 4), reader.GetString(offset + 5),
        reader.GetBoolean(offset + 6), reader.GetInt64(offset + 7));
}
