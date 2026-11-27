using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Sisplan.Api;

internal static class MapaEstrategicoEndpoints
{
    public static void MapMapaEstrategico(this RouteGroupBuilder pei)
    {
        pei.MapGet("/peis/{peiId:guid}/mapa", async (Guid peiId, Guid? versionId,
            MapaEstrategicoRepository repository, CancellationToken ct) =>
        {
            var mapa = await repository.FindAsync(peiId, versionId, ct);
            return mapa is null
                ? Results.Problem(statusCode: 404, title: "No se encontró la versión del PEI solicitada.")
                : Results.Ok(mapa);
        });
    }
}

internal sealed record MapaEstrategicoDto(Guid PeiId, Guid VersionId, string Estado, IReadOnlyList<EjeMapaDto> Ejes);

internal sealed record EjeMapaDto(Guid Id, string Nombre, string? Descripcion, bool Activo, long Revision)
{
    public List<ObjetivoMapaDto> Objetivos { get; } = [];
}

internal sealed record ObjetivoMapaDto(Guid Id, string Codigo, string Nombre, string Descripcion, string ResultadoEsperado,
    string LineaAccion, bool Activo, long Revision)
{
    public List<IndicadorDto> Indicadores { get; } = [];
    public List<RiesgoDto> Riesgos { get; } = [];
}

internal sealed class MapaEstrategicoRepository(SesionInstitucional source)
{
    public async Task<MapaEstrategicoDto?> FindAsync(Guid peiId, Guid? versionId, CancellationToken ct)
    {
        // Las cinco lecturas usan la misma instantánea, incluso si el PEI cambia de estado en paralelo.
        var connection = source.Conexion;
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var version = await FindVersionAsync(peiId, versionId, connection, transaction, ct);
        if (version is null)
            return null;
        var ejes = new Dictionary<Guid, EjeMapaDto>();
        var objetivos = new Dictionary<Guid, ObjetivoMapaDto>();
        Guid id = version.Value.Id;
        using (var batch = new NpgsqlBatch(connection, transaction))
        {
            Add(batch, id, """
                SELECT e.id, v.nombre, v.descripcion, v.activo, v.revision
                FROM sisplan.eje e
                JOIN LATERAL (
                    SELECT nombre, descripcion, activo, revision
                    FROM sisplan.eje_version WHERE eje_id=e.id ORDER BY revision DESC LIMIT 1
                ) v ON true
                WHERE e.pei_version_id=@version
                ORDER BY e.creado_en,e.id
                """);
            Add(batch, id, $"""
                SELECT eje_id, {ObjetivoRepository.Fields}
                FROM sisplan.objetivo
                WHERE eje_id IN (SELECT id FROM sisplan.eje WHERE pei_version_id=@version)
                ORDER BY codigo,id
                """);
            Add(batch, id, $"""
                SELECT i.objetivo_id, {IndicadorRepository.Fields}
                FROM sisplan.indicador i
                JOIN sisplan.objetivo o ON o.id=i.objetivo_id
                JOIN sisplan.eje e ON e.id=o.eje_id
                WHERE e.pei_version_id=@version
                ORDER BY i.nombre,i.id
                """);
            Add(batch, id, $"""
                SELECT {RiesgoEstrategicoRepository.Fields}
                FROM sisplan.riesgo_estrategico
                WHERE objetivo_id IN (
                    SELECT o.id FROM sisplan.objetivo o JOIN sisplan.eje e ON e.id=o.eje_id WHERE e.pei_version_id=@version
                )
                ORDER BY nombre,id
                """);

            await using var reader = await batch.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var eje = new EjeMapaDto(reader.GetGuid(0), reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetBoolean(3), reader.GetInt64(4));
                ejes.Add(eje.Id, eje);
            }

            await reader.NextResultAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var dto = ObjetivoRepository.Read(reader, 1);
                var objetivo = new ObjetivoMapaDto(dto.Id, dto.Codigo, dto.Nombre, dto.Descripcion, dto.ResultadoEsperado,
                    dto.LineaAccion, dto.Activo, dto.Revision);
                objetivos.Add(objetivo.Id, objetivo);
                ejes[reader.GetGuid(0)].Objetivos.Add(objetivo);
            }

            await reader.NextResultAsync(ct);
            while (await reader.ReadAsync(ct))
                objetivos[reader.GetGuid(0)].Indicadores.Add(IndicadorRepository.Read(reader, 1));

            await reader.NextResultAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var riesgo = RiesgoEstrategicoRepository.Read(reader);
                objetivos[riesgo.ObjetivoId].Riesgos.Add(riesgo);
            }
        }
        await transaction.CommitAsync(ct);
        return new MapaEstrategicoDto(peiId, id, version.Value.Estado, ejes.Values.ToArray());
    }

    private static async Task<(Guid Id, string Estado)?> FindVersionAsync(Guid peiId, Guid? versionId,
        NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            SELECT id,estado FROM sisplan.pei_version
            WHERE pei_id=@pei AND (@version IS NULL OR id=@version)
            ORDER BY numero DESC LIMIT 1
            """, connection, transaction);
        command.Parameters.AddWithValue("pei", peiId);
        command.Parameters.AddWithValue("version", NpgsqlDbType.Uuid, (object?)versionId ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetGuid(0), reader.GetString(1)) : null;
    }

    private static void Add(NpgsqlBatch batch, Guid versionId, string sql)
    {
        var command = new NpgsqlBatchCommand(sql);
        command.Parameters.AddWithValue("version", versionId);
        batch.BatchCommands.Add(command);
    }
}
