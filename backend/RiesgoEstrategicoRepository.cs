using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class RiesgoEstrategicoRepository(SesionInstitucional source, ObjetivoRepository objectives)
{
    internal const string Fields = """
        id, objetivo_id, nombre, descripcion, probabilidad, impacto, tratamiento, accion_mitigacion,
        responsable, fecha_revision, evidencia_url, cerrado, revision
        """;

    public async Task<IReadOnlyList<RiesgoDto>?> ListAsync(Guid peiId, Guid ejeId, Guid objetivoId, CancellationToken ct)
    {
        if (await objectives.FindAsync(peiId, ejeId, objetivoId, ct) is null)
            return null;
        await using var command = source.CreateCommand($"""
            SELECT {Fields} FROM sisplan.riesgo_estrategico WHERE objetivo_id=@objetivo ORDER BY nombre,id
            """);
        command.Parameters.AddWithValue("objetivo", objetivoId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<RiesgoDto>();
        while (await reader.ReadAsync(ct))
            result.Add(Read(reader));
        return result;
    }

    public async Task<RiesgoDto?> FindAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid id, CancellationToken ct)
    {
        if (await objectives.FindAsync(peiId, ejeId, objetivoId, ct) is null)
            return null;
        return await ReadAsync(objetivoId, id, source.Conexion, null, ct);
    }

    public async Task<RiesgoWriteResult> WriteAsync(Guid peiId, Guid ejeId, RiesgoDto fields, string? expectedTag,
        CancellationToken ct)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var parent = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, ct);
        if (parent is null)
            return new(RiesgoWriteStatus.NotFound);
        var eje = await EjeRepository.FindCurrentAsync(parent.Value.Id, ejeId, connection, transaction, ct);
        if (eje is null)
            return new(RiesgoWriteStatus.NotFound);
        await using var objective = new NpgsqlCommand(
            "SELECT activo FROM sisplan.objetivo WHERE eje_id=@eje AND id=@id", connection, transaction);
        objective.Parameters.AddWithValue("eje", ejeId);
        objective.Parameters.AddWithValue("id", fields.ObjetivoId);
        if (await objective.ExecuteScalarAsync(ct) is not bool active)
            return new(RiesgoWriteStatus.NotFound);

        RiesgoDto? current = null;
        if (expectedTag is not null)
        {
            current = await ReadAsync(fields.ObjetivoId, fields.Id, connection, transaction, ct);
            if (current is null)
                return new(RiesgoWriteStatus.NotFound);
            if (expectedTag != current.ETag())
                return new(RiesgoWriteStatus.Stale);
        }
        if (!PeiEstados.Editable(parent.Value.State))
            return new(RiesgoWriteStatus.ReadOnlyPei);
        if (!eje.Activo || !active)
            return new(RiesgoWriteStatus.InactiveStructure);
        if (current is not null && fields with { Revision = current.Revision } == current)
            return new(RiesgoWriteStatus.Saved, current);

        var sql = current is null
            ? $"""
                INSERT INTO sisplan.riesgo_estrategico (id, objetivo_id, nombre, descripcion, probabilidad, impacto,
                    tratamiento, accion_mitigacion, responsable, fecha_revision, evidencia_url, cerrado)
                VALUES (@id, @objetivo, @nombre, @descripcion, @probabilidad, @impacto, @tratamiento,
                    @accion, @responsable, @fecha, @evidencia, @cerrado)
                RETURNING {Fields}
                """
            : $"""
                UPDATE sisplan.riesgo_estrategico
                SET nombre=@nombre, descripcion=@descripcion, probabilidad=@probabilidad, impacto=@impacto,
                    tratamiento=@tratamiento, accion_mitigacion=@accion, responsable=@responsable, fecha_revision=@fecha,
                    evidencia_url=@evidencia, cerrado=@cerrado, revision=revision+1
                WHERE id=@id AND objetivo_id=@objetivo
                RETURNING {Fields}
                """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", fields.Id);
        command.Parameters.AddWithValue("objetivo", fields.ObjetivoId);
        command.Parameters.AddWithValue("nombre", fields.Nombre);
        command.Parameters.AddWithValue("descripcion", fields.Descripcion);
        command.Parameters.AddWithValue("probabilidad", fields.Probabilidad);
        command.Parameters.AddWithValue("impacto", fields.Impacto);
        command.Parameters.AddWithValue("tratamiento", NpgsqlDbType.Text, (object?)fields.Tratamiento ?? DBNull.Value);
        command.Parameters.AddWithValue("accion", NpgsqlDbType.Text, (object?)fields.AccionMitigacion ?? DBNull.Value);
        command.Parameters.AddWithValue("responsable", NpgsqlDbType.Text, (object?)fields.Responsable ?? DBNull.Value);
        command.Parameters.AddWithValue("fecha", NpgsqlDbType.Date, (object?)fields.FechaRevision ?? DBNull.Value);
        command.Parameters.AddWithValue("evidencia", NpgsqlDbType.Text, (object?)fields.EvidenciaUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("cerrado", fields.Cerrado);
        var saved = await ReadAsync(command, ct);
        if (saved is null)
            return new(RiesgoWriteStatus.NotFound);
        await transaction.CommitAsync(ct);
        return new(RiesgoWriteStatus.Saved, saved);
    }

    private static async Task<RiesgoDto?> ReadAsync(Guid objetivoId, Guid id, NpgsqlConnection connection,
        NpgsqlTransaction? transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand($"""
            SELECT {Fields} FROM sisplan.riesgo_estrategico WHERE objetivo_id=@objetivo AND id=@id
            """, connection, transaction);
        command.Parameters.AddWithValue("objetivo", objetivoId);
        command.Parameters.AddWithValue("id", id);
        return await ReadAsync(command, ct);
    }

    private static async Task<RiesgoDto?> ReadAsync(NpgsqlCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Read(reader) : null;
    }

    internal static RiesgoDto Read(NpgsqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
        reader.GetInt32(4), reader.GetInt32(5), Text(reader, 6), Text(reader, 7), Text(reader, 8),
        reader.IsDBNull(9) ? null : reader.GetFieldValue<DateOnly>(9), Text(reader, 10), reader.GetBoolean(11),
        reader.GetInt64(12));

    private static string? Text(NpgsqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
}
