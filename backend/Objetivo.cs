using System.Globalization;

namespace Sisplan.Api;

internal sealed record ObjetivoInput(
    string? Codigo, string? Nombre, string? Descripcion, string? ResultadoEsperado, string? LineaAccion);

internal sealed record ObjetivoDto(
    Guid Id, string Codigo, string Nombre, string Descripcion, string ResultadoEsperado, string LineaAccion,
    bool Activo = true, long Revision = 1)
{
    public string ETag() => $"\"objetivo-{Id:N}-{Revision.ToString(CultureInfo.InvariantCulture)}\"";
}

internal sealed record ObjetivoEstadoInput(bool? Activo);

internal enum ObjetivoCreateStatus { Saved, NotFound, ReadOnlyPei, InactiveAxis, DuplicateCode }

internal sealed record ObjetivoCreateResult(ObjetivoCreateStatus Status, ObjetivoDto? Objetivo = null);

internal enum ObjetivoEstadoStatus { Saved, NotFound, ReadOnlyPei, InactiveAxis, Stale }

internal sealed record ObjetivoEstadoResult(ObjetivoEstadoStatus Status, ObjetivoDto? Objetivo = null);
