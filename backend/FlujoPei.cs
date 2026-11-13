namespace Sisplan.Api;

internal sealed record FlujoPeiInput(string? Accion, string? Motivo);
internal sealed record FlujoPeiActor(string Emisor, string Sujeto);
internal sealed record FlujoPeiOpciones(IReadOnlyList<string> Acciones, string? MotivoDevolucion);
internal sealed record FlujoPeiEvaluacionDto(Guid Id, int Cuatrimestre, DateOnly FechaInicio, DateOnly FechaFin, string Estado);
internal sealed record FlujoPeiPoaDto(Guid Id, int Anio, string Estado, DateOnly FechaInicio, DateOnly FechaFin,
    bool Vigente, List<FlujoPeiEvaluacionDto> Evaluaciones);

internal enum FlujoPeiStatus
{
    Saved,
    NotFound,
    Stale,
    Conflict,
    SelfApproval,
    IncompleteStructure,
    OverlappingPeriod
}

internal sealed record FlujoPeiResult(FlujoPeiStatus Status, PeiSnapshot? Snapshot = null);
