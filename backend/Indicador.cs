namespace Sisplan.Api;

internal sealed record IndicadorInput(string? Nombre, string? Formula, string? Unidad, decimal? LineaBase,
    decimal? Meta, string? Frecuencia);

internal sealed record IndicadorDto(Guid Id, string Nombre, string Formula, string Unidad, decimal LineaBase,
    decimal Meta, string Frecuencia);

internal sealed record IndicadorMedicionInput(DateOnly? Periodo, decimal? Valor);

internal sealed record IndicadorMedicionDto(Guid Id, DateOnly Periodo, decimal Valor);

internal enum IndicadorWriteStatus
{
    Saved,
    NotFound,
    ReadOnlyPei,
    InactiveAxis,
    OutsidePlan,
    DuplicatePeriod,
}
