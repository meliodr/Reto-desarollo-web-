using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sisplan.Api;

internal sealed record IndicadorInput(string? Nombre, string? Formula, string? Unidad, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? LineaBase,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? Meta, string? Frecuencia);

internal sealed record IndicadorDto(Guid Id, string Nombre, string Formula, string Unidad, [property: JsonConverter(typeof(IndicadorDecimalConverter))] decimal LineaBase,
    [property: JsonConverter(typeof(IndicadorDecimalConverter))] decimal Meta, string Frecuencia);

internal sealed record IndicadorMedicionInput(DateOnly? Periodo,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal? Valor);

internal sealed record IndicadorMedicionDto(Guid Id, DateOnly Periodo,
    [property: JsonConverter(typeof(IndicadorDecimalConverter))] decimal Valor);

internal enum IndicadorWriteStatus
{
    Saved,
    NotFound,
    ReadOnlyPei,
    InactiveAxis,
    OutsidePlan,
    DuplicatePeriod,
}


internal sealed class IndicadorDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        reader.GetDecimal();

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("0.####", CultureInfo.InvariantCulture));
}
