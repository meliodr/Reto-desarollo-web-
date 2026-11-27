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

    public Task<ObjetivoUpdateResult> SetActiveAsync(Guid peiId, Guid ejeId, Guid objetivoId, string expectedTag,
        bool active, CancellationToken ct) => ChangeAsync(peiId, ejeId, objetivoId, expectedTag, null, active, ct);

    public Task<ObjetivoUpdateResult> UpdateAsync(Guid peiId, Guid ejeId, ObjetivoDto fields, string expectedTag,
        CancellationToken ct) => ChangeAsync(peiId, ejeId, fields.Id, expectedTag, fields, null, ct);

    private async Task<ObjetivoUpdateResult> ChangeAsync(Guid peiId, Guid ejeId, Guid objetivoId, string expectedTag,
        ObjetivoDto? fields, bool? active, CancellationToken ct)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, ct);
        if (parent is null)
            return new(ObjetivoUpdateStatus.NotFound);
        var axis = await EjeRepository.FindCurrentAsync(parent.Value.Id, ejeId, connection, transaction, ct);
        if (axis is null)
            return new(ObjetivoUpdateStatus.NotFound);

        await using var find = new NpgsqlCommand($"SELECT {Fields} FROM sisplan.objetivo WHERE eje_id=@eje AND id=@id",
            connection, transaction);
        find.Parameters.AddWithValue("eje", ejeId);
        find.Parameters.AddWithValue("id", objetivoId);
        var current = await ReadOneAsync(find, ct);
        if (current is null)
            return new(ObjetivoUpdateStatus.NotFound);
        if (expectedTag != current.ETag())
            return new(ObjetivoUpdateStatus.Stale);
        if (!PeiEstados.Editable(parent.Value.State))
            return new(ObjetivoUpdateStatus.ReadOnlyPei);
        if (!axis.Activo && (fields is not null || active is true))
            return new(ObjetivoUpdateStatus.InactiveAxis);
        if (fields is not null && !current.Activo)
            return new(ObjetivoUpdateStatus.InactiveObjective);

        var updated = (fields ?? current) with { Activo = active ?? current.Activo, Revision = current.Revision };
        if (updated == current)
            return new(ObjetivoUpdateStatus.Saved, current);

        if (updated.Codigo != current.Codigo)
        {
            // El bloqueo del PEI abarca también las altas y los demás cambios de código de sus objetivos.
            await using var duplicate = new NpgsqlCommand("""
                SELECT EXISTS (SELECT 1 FROM sisplan.objetivo WHERE eje_id=@eje AND codigo=@codigo AND id<>@id)
                """, connection, transaction);
            duplicate.Parameters.AddWithValue("eje", ejeId);
            duplicate.Parameters.AddWithValue("codigo", updated.Codigo);
            duplicate.Parameters.AddWithValue("id", objetivoId);
            if (await duplicate.ExecuteScalarAsync(ct) is true)
                return new(ObjetivoUpdateStatus.DuplicateCode);
        }

        await using var update = new NpgsqlCommand($"""
            UPDATE sisplan.objetivo SET codigo=@codigo, nombre=@nombre, descripcion=@descripcion,
                resultado_esperado=@resultado, linea_accion=@linea, activo=@activo, revision=revision+1
            WHERE eje_id=@eje AND id=@id RETURNING {Fields}
            """, connection, transaction);
        update.Parameters.AddWithValue("codigo", updated.Codigo);
        update.Parameters.AddWithValue("nombre", updated.Nombre);
        update.Parameters.AddWithValue("descripcion", updated.Descripcion);
        update.Parameters.AddWithValue("resultado", updated.ResultadoEsperado);
        update.Parameters.AddWithValue("linea", updated.LineaAccion);
        update.Parameters.AddWithValue("activo", updated.Activo);
        update.Parameters.AddWithValue("eje", ejeId);
        update.Parameters.AddWithValue("id", objetivoId);
        var saved = await ReadOneAsync(update, ct);
        if (saved is null)
            return new(ObjetivoUpdateStatus.NotFound);
        await transaction.CommitAsync(ct);
        return new(ObjetivoUpdateStatus.Saved, saved);
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
