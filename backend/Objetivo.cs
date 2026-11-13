namespace Sisplan.Api;

internal sealed record ObjetivoInput(
    string? Codigo, string? Nombre, string? Descripcion, string? ResultadoEsperado, string? LineaAccion);

internal sealed record ObjetivoDto(
    Guid Id, string Codigo, string Nombre, string Descripcion, string ResultadoEsperado, string LineaAccion);

internal enum ObjetivoCreateStatus { Saved, NotFound, ReadOnlyPei, InactiveAxis, DuplicateCode }

internal sealed record ObjetivoCreateResult(ObjetivoCreateStatus Status, ObjetivoDto? Objetivo = null);
