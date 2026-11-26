using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal sealed class IndicadorRepository(SesionInstitucional source)
{
    internal const string Fields = """
        i.id, i.nombre, i.formula, i.unidad, i.linea_base, i.meta, i.frecuencia,
        i.sentido, i.metodo_calculo, i.listo_para_medir, i.revision
        """;

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
        var sql = CurrentObjective + $"""

            SELECT {Fields}
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
                result.Add(Read(reader));
        } while (await reader.ReadAsync(cancellationToken));
        return result;
    }

    public async Task<IndicadorDto?> FindAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid indicadorId,
        CancellationToken cancellationToken)
    {
        await using var command = source.CreateCommand(CurrentObjective + $"""

            SELECT {Fields}
            FROM objetivo_actual o JOIN sisplan.indicador i ON i.objetivo_id = o.id
            WHERE i.id = @indicadorId
            """);
        AddObjectivePath(command, peiId, ejeId, objetivoId);
        command.Parameters.AddWithValue("indicadorId", indicadorId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    internal static IndicadorDto Read(NpgsqlDataReader reader, int offset = 0) => new(
        reader.GetGuid(offset),
        reader.GetString(offset + 1),
        ReadText(reader, offset + 2),
        ReadText(reader, offset + 3),
        reader.IsDBNull(offset + 4) ? null : reader.GetDecimal(offset + 4),
        reader.IsDBNull(offset + 5) ? null : reader.GetDecimal(offset + 5),
        ReadText(reader, offset + 6),
        ReadText(reader, offset + 7),
        ReadText(reader, offset + 8),
        reader.GetBoolean(offset + 9),
        reader.GetInt64(offset + 10));

    private static string? ReadText(NpgsqlDataReader reader, int index) =>
        reader.IsDBNull(index) ? null : reader.GetString(index);

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
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockObjectiveAsync(peiId, ejeId, objetivoId, connection, transaction, cancellationToken);
        var status = CheckState(parent);
        if (status != IndicadorWriteStatus.Saved)
            return status;

        const string sql = """
            INSERT INTO sisplan.indicador
                (id, objetivo_id, nombre, formula, unidad, linea_base, meta, frecuencia, sentido, metodo_calculo)
            VALUES (@id, @objetivoId, @nombre, @formula, @unidad, @lineaBase, @meta, @frecuencia, @sentido, @metodo)
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", data.Id);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
        AddFields(command, data);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return IndicadorWriteStatus.Saved;
    }

    public async Task<(IndicadorWriteStatus Status, IndicadorDto? Data)> UpdateAsync(
        Guid peiId, Guid ejeId, Guid objetivoId, IndicadorDto data, string expected, CancellationToken cancellationToken)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockObjectiveAsync(peiId, ejeId, objetivoId, connection, transaction, cancellationToken);
        if (parent is null)
            return (IndicadorWriteStatus.NotFound, null);

        const string lookup = """
            SELECT revision FROM sisplan.indicador
            WHERE id = @id AND objetivo_id = @objetivoId
            FOR UPDATE
            """;
        await using var find = new NpgsqlCommand(lookup, connection, transaction);
        find.Parameters.AddWithValue("id", data.Id);
        find.Parameters.AddWithValue("objetivoId", objetivoId);
        if (await find.ExecuteScalarAsync(cancellationToken) is not long revision)
            return (IndicadorWriteStatus.NotFound, null);
        if (!string.Equals(expected, (data with { Revision = revision }).ETag(), StringComparison.Ordinal))
            return (IndicadorWriteStatus.Stale, null);

        var status = CheckState(parent);
        if (status != IndicadorWriteStatus.Saved)
            return (status, null);

        const string sql = """
            UPDATE sisplan.indicador SET nombre=@nombre, formula=@formula, unidad=@unidad,
                linea_base=@lineaBase, meta=@meta, frecuencia=@frecuencia,
                sentido=@sentido, metodo_calculo=@metodo, revision=revision+1
            WHERE id=@id AND objetivo_id=@objetivoId
            RETURNING revision, listo_para_medir
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", data.Id);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
        AddFields(command, data);
        IndicadorDto saved;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                return (IndicadorWriteStatus.NotFound, null);
            saved = data with { Revision = reader.GetInt64(0), ListoParaMedir = reader.GetBoolean(1) };
        }
        await transaction.CommitAsync(cancellationToken);
        return (IndicadorWriteStatus.Saved, saved);
    }

    public async Task<IndicadorWriteStatus> CreateMeasurementAsync(Guid peiId, Guid ejeId, Guid objetivoId,
        Guid indicadorId, IndicadorMedicionDto data, CancellationToken cancellationToken)
    {
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var parent = await LockObjectiveAsync(peiId, ejeId, objetivoId, connection, transaction, cancellationToken);
        if (parent is null)
            return IndicadorWriteStatus.NotFound;

        const string check = """
            SELECT @periodo BETWEEN p.fecha_inicio AND p.fecha_fin, i.listo_para_medir
            FROM sisplan.indicador i CROSS JOIN sisplan.pei_version p
            WHERE i.id = @indicadorId AND i.objetivo_id = @objetivoId AND p.id = @peiVersionId
            """;
        await using var lookup = new NpgsqlCommand(check, connection, transaction);
        lookup.Parameters.AddWithValue("indicadorId", indicadorId);
        lookup.Parameters.AddWithValue("objetivoId", objetivoId);
        lookup.Parameters.AddWithValue("peiVersionId", parent.Value.Id);
        lookup.Parameters.AddWithValue("periodo", NpgsqlDbType.Date, data.Periodo);
        bool insidePlan;
        bool configured;
        await using (var reader = await lookup.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                return IndicadorWriteStatus.NotFound;
            insidePlan = reader.GetBoolean(0);
            configured = reader.GetBoolean(1);
        }

        var status = CheckState(parent);
        if (status != IndicadorWriteStatus.Saved)
            return status;
        if (!configured)
            return IndicadorWriteStatus.PendingConfiguration;
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

    private static async Task<(Guid Id, string State, bool AxisActive, bool ObjectiveActive)?> LockObjectiveAsync(Guid peiId, Guid ejeId,
        Guid objetivoId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        var plan = await EjeRepository.LockCurrentPeiVersionAsync(peiId, connection, transaction, cancellationToken);
        if (plan is null)
            return null;
        var axis = await EjeRepository.FindCurrentAsync(plan.Value.Id, ejeId, connection, transaction, cancellationToken);
        if (axis is null)
            return null;

        const string sql = "SELECT activo FROM sisplan.objetivo WHERE id = @objetivoId AND eje_id = @ejeId";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("ejeId", ejeId);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
        return await command.ExecuteScalarAsync(cancellationToken) is bool objectiveActive
            ? (plan.Value.Id, plan.Value.State, axis.Activo, objectiveActive)
            : null;
    }

    private static IndicadorWriteStatus CheckState((Guid Id, string State, bool AxisActive, bool ObjectiveActive)? parent)
    {
        if (parent is null)
            return IndicadorWriteStatus.NotFound;
        if (!PeiEstados.Editable(parent.Value.State))
            return IndicadorWriteStatus.ReadOnlyPei;
        if (!parent.Value.AxisActive)
            return IndicadorWriteStatus.InactiveAxis;
        return parent.Value.ObjectiveActive ? IndicadorWriteStatus.Saved : IndicadorWriteStatus.InactiveObjective;
    }

    private static void AddFields(NpgsqlCommand command, IndicadorDto data)
    {
        command.Parameters.AddWithValue("nombre", data.Nombre);
        command.Parameters.AddWithValue("formula", data.Formula!);
        command.Parameters.AddWithValue("unidad", data.Unidad!);
        command.Parameters.AddWithValue("lineaBase", NpgsqlDbType.Numeric, (object?)data.LineaBase ?? DBNull.Value);
        command.Parameters.AddWithValue("meta", data.Meta!.Value);
        command.Parameters.AddWithValue("frecuencia", data.Frecuencia!);
        command.Parameters.AddWithValue("sentido", data.Sentido!);
        command.Parameters.AddWithValue("metodo", data.MetodoCalculo!);
    }

    private static void AddObjectivePath(NpgsqlCommand command, Guid peiId, Guid ejeId, Guid objetivoId)
    {
        command.Parameters.AddWithValue("peiId", peiId);
        command.Parameters.AddWithValue("ejeId", ejeId);
        command.Parameters.AddWithValue("objetivoId", objetivoId);
    }
}
