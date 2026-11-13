using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class IndicadorRepository(NpgsqlDataSource source)
{
    private const string CurrentObjective = """
        WITH objetivo_actual AS (
            SELECT o.id
            FROM sisplan.pei p
            JOIN LATERAL (
                SELECT id FROM sisplan.pei_version
                WHERE pei_id = p.id ORDER BY numero DESC LIMIT 1
            ) v ON true
            JOIN sisplan.eje e ON e.pei_version_id = v.id
            JOIN sisplan.objetivo o ON o.eje_id = e.id
            WHERE p.id = @peiId AND e.id = @ejeId AND o.id = @objetivoId
        )
        """;

    public async Task<IReadOnlyList<IndicadorDto>?> ListAsync(Guid peiId, Guid ejeId, Guid objetivoId,
        CancellationToken cancellationToken)
    {
        var sql = CurrentObjective + """

            SELECT i.id, i.nombre, i.formula, i.unidad, i.linea_base, i.meta, i.frecuencia
            FROM objetivo_actual o LEFT JOIN sisplan.indicador i ON i.objetivo_id = o.id
            ORDER BY i.nombre, i.id
            """;
        await using var command = source.CreateCommand(sql);
        AddObjectivePath(command, peiId, ejeId, objetivoId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var result = new List<IndicadorDto>();
        do
        {
            if (!reader.IsDBNull(0))
                result.Add(new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetDecimal(4), reader.GetDecimal(5), reader.GetString(6)));
        } while (await reader.ReadAsync(cancellationToken));
        return result;
    }

    public async Task<IReadOnlyList<IndicadorMedicionDto>?> ListMeasurementsAsync(Guid peiId, Guid ejeId,
        Guid objetivoId, Guid indicadorId, CancellationToken cancellationToken)
    {
        var sql = CurrentObjective + """

            SELECT m.id, m.periodo, m.valor
            FROM objetivo_actual o JOIN sisplan.indicador i ON i.objetivo_id = o.id
            LEFT JOIN sisplan.indicador_medicion m ON m.indicador_id = i.id
            WHERE i.id = @indicadorId
            ORDER BY m.periodo DESC
            """;
        await using var command = source.CreateCommand(sql);
        AddObjectivePath(command, peiId, ejeId, objetivoId);
        command.Parameters.AddWithValue("indicadorId", indicadorId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        var result = new List<IndicadorMedicionDto>();
        do
        {
            if (!reader.IsDBNull(0))
                result.Add(new(reader.GetGuid(0), reader.GetFieldValue<DateOnly>(1), reader.GetDecimal(2)));
        } while (await reader.ReadAsync(cancellationToken));
        return result;
    }

    public async Task<IndicadorWriteStatus> CreateAsync(Guid peiId, Guid ejeId, Guid objetivoId, IndicadorDto data,
        CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockObjectiveAsync(peiId, ejeId, objetivoId, connection, transaction, cancellationToken);
        var status = CheckState(parent);
        if (status != IndicadorWriteStatus.Saved)
            return status;

        const string sql = """
            INSERT INTO sisplan.indicador (id, objetivo_id, nombre, formula, unidad, linea_base, meta, frecuencia)
            VALUES (@id, @objetivoId, @nombre, @formula, @unidad, @lineaBase, @meta, @frecuencia)
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", data.Id);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
        command.Parameters.AddWithValue("nombre", data.Nombre);
        command.Parameters.AddWithValue("formula", data.Formula);
        command.Parameters.AddWithValue("unidad", data.Unidad);
        command.Parameters.AddWithValue("lineaBase", data.LineaBase);
        command.Parameters.AddWithValue("meta", data.Meta);
        command.Parameters.AddWithValue("frecuencia", data.Frecuencia);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return IndicadorWriteStatus.Saved;
    }

    public async Task<IndicadorWriteStatus> CreateMeasurementAsync(Guid peiId, Guid ejeId, Guid objetivoId,
        Guid indicadorId, IndicadorMedicionDto data, CancellationToken cancellationToken)
    {
        await using var connection = await source.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockObjectiveAsync(peiId, ejeId, objetivoId, connection, transaction, cancellationToken);
        if (parent is null)
            return IndicadorWriteStatus.NotFound;

        const string check = """
            SELECT @periodo BETWEEN p.fecha_inicio AND p.fecha_fin
            FROM sisplan.indicador i CROSS JOIN sisplan.pei_version p
            WHERE i.id = @indicadorId AND i.objetivo_id = @objetivoId AND p.id = @peiVersionId
            """;
        await using var lookup = new NpgsqlCommand(check, connection, transaction);
        lookup.Parameters.AddWithValue("indicadorId", indicadorId);
        lookup.Parameters.AddWithValue("objetivoId", objetivoId);
        lookup.Parameters.AddWithValue("peiVersionId", parent.Value.Id);
        lookup.Parameters.AddWithValue("periodo", NpgsqlDbType.Date, data.Periodo);
        if (await lookup.ExecuteScalarAsync(cancellationToken) is not bool insidePlan)
            return IndicadorWriteStatus.NotFound;

        var status = CheckState(parent);
        if (status != IndicadorWriteStatus.Saved)
            return status;
        if (!insidePlan)
            return IndicadorWriteStatus.OutsidePlan;

        const string sql = """
            INSERT INTO sisplan.indicador_medicion (id, indicador_id, periodo, valor)
            VALUES (@id, @indicadorId, @periodo, @valor)
            ON CONFLICT ON CONSTRAINT indicador_medicion_periodo_unico DO NOTHING
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", data.Id);
        command.Parameters.AddWithValue("indicadorId", indicadorId);
        command.Parameters.AddWithValue("periodo", NpgsqlDbType.Date, data.Periodo);
        command.Parameters.AddWithValue("valor", data.Valor);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            return IndicadorWriteStatus.DuplicatePeriod;
        await transaction.CommitAsync(cancellationToken);
        return IndicadorWriteStatus.Saved;
    }

    private static async Task<(Guid Id, string State, bool AxisActive)?> LockObjectiveAsync(Guid peiId, Guid ejeId,
        Guid objetivoId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        var plan = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, cancellationToken);
        if (plan is null)
            return null;
        var axis = await EjeRepository.FindCurrentAsync(plan.Value.Id, ejeId, connection, transaction, cancellationToken);
        if (axis is null)
            return null;

        const string sql = "SELECT id FROM sisplan.objetivo WHERE id = @objetivoId AND eje_id = @ejeId";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("ejeId", ejeId);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
        return await command.ExecuteScalarAsync(cancellationToken) is Guid
            ? (plan.Value.Id, plan.Value.State, axis.Activo)
            : null;
    }

    private static IndicadorWriteStatus CheckState((Guid Id, string State, bool AxisActive)? parent)
    {
        if (parent is null)
            return IndicadorWriteStatus.NotFound;
        if (parent.Value.State != "borrador")
            return IndicadorWriteStatus.ReadOnlyPei;
        return parent.Value.AxisActive ? IndicadorWriteStatus.Saved : IndicadorWriteStatus.InactiveAxis;
    }

    private static void AddObjectivePath(NpgsqlCommand command, Guid peiId, Guid ejeId, Guid objetivoId)
    {
        command.Parameters.AddWithValue("peiId", peiId);
        command.Parameters.AddWithValue("ejeId", ejeId);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
    }
}
