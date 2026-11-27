using System.Text.Json.Serialization;

namespace Sisplan.Api;

internal sealed record FlujoPeiInput(string? Accion, string? Motivo);
internal sealed record PeiRevisionInput(string? Motivo);
internal sealed record FlujoPeiActor(string Emisor, string Sujeto, bool Analista, bool Director, bool Comite, bool Autoridad);
internal sealed record FlujoPeiOpciones(IReadOnlyList<string> Acciones, string? MotivoDevolucion,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? IndicadoresDigemapsPendientes);
internal sealed record FlujoPeiTransicionDto(long Revision, string Accion, string EstadoAnterior, string EstadoNuevo,
    string Emisor, string Sujeto, string? Motivo, DateTime RegistradaEn);
internal sealed record FlujoPeiEvaluacionDto(Guid Id, int Cuatrimestre, DateOnly FechaInicio, DateOnly FechaFin, string Estado);
internal sealed record FlujoPeiPoaDto(Guid Id, int Anio, string Estado, DateOnly FechaInicio, DateOnly FechaFin,
    bool Vigente, List<FlujoPeiEvaluacionDto> Evaluaciones);

internal enum FlujoPeiStatus
{
    Saved,
    NotFound,
    Stale,
    Conflict,
    Forbidden,
    IncompleteStructure,
    PendingManualIndicators,
    OverlappingPeriod
}

internal sealed record FlujoPeiResult(FlujoPeiStatus Status, PeiSnapshot? Snapshot = null);

internal static class PeiEstados
{
    public static bool Editable(string state) => state is "borrador" or "observado";
}
