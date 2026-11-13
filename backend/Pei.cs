using System.Globalization;

namespace Sisplan.Api;

internal sealed record PeiInput(
    string? Nombre,
    string? Tipo,
    string? FechaInicio,
    string? FechaFin,
    string? Mision,
    string? Vision,
    string? Valores);

internal sealed record PeiFields(
    string Nombre,
    string Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string Mision,
    string Vision,
    string Valores);

internal sealed record PeiDto(
    Guid Id,
    Guid VersionId,
    string Nombre,
    string Tipo,
    DateOnly FechaInicio,
    DateOnly FechaFin,
    string Mision,
    string Vision,
    string Valores,
    string Estado);

internal sealed record PeiSnapshot(PeiDto Pei, long Revision)
{
    public string ETag => $"\"pei-{Pei.VersionId:N}-{Revision.ToString(CultureInfo.InvariantCulture)}\"";
}

internal static class PeiValidation
{
    public static (PeiFields? Fields, Dictionary<string, string[]> Errors) Validate(PeiInput input)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var nombre = RequiredText(input.Nombre, "nombre", "nombre", 3, 200, errors);
        var mision = RequiredText(input.Mision, "mision", "misión", 1, 4000, errors);
        var vision = RequiredText(input.Vision, "vision", "visión", 1, 4000, errors);
        var valores = RequiredText(input.Valores, "valores", "valores", 1, 4000, errors);
        var tipo = input.Tipo?.Trim() ?? "";
        var requiredYears = tipo switch { "ordinario" => 4, "extraordinario" => 5, _ => 0 };
        if (requiredYears == 0)
            errors["tipo"] = ["Selecciona ordinario o extraordinario."];

        var hasStart = DateOnly.TryParseExact(input.FechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start);
        var hasEnd = DateOnly.TryParseExact(input.FechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end);
        if (!hasStart)
            errors["fechaInicio"] = ["Indica una fecha de inicio válida en formato AAAA-MM-DD."];
        if (!hasEnd)
            errors["fechaFin"] = ["Indica una fecha de fin válida en formato AAAA-MM-DD."];

        if (hasStart && hasEnd)
        {
            if (end < start)
                errors["fechaFin"] = ["La fecha de fin debe ser igual o posterior a la fecha de inicio."];
            else if (requiredYears > 0 && end.Year - start.Year + 1 != requiredYears)
                errors["fechaFin"] = [$"Un PEI {tipo} debe abarcar {requiredYears} años calendario inclusivos."];
        }

        return errors.Count == 0
            ? (new PeiFields(nombre, tipo, start, end, mision, vision, valores), errors)
            : (null, errors);
    }

    private static string RequiredText(string? value, string key, string label, int min, int max, Dictionary<string, string[]> errors)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length < min || normalized.Length > max)
            errors[key] = [$"El campo {label} debe contener entre {min} y {max} caracteres."];
        else if (normalized.Contains('\0'))
            errors[key] = [$"El campo {label} contiene un carácter no permitido."];
        return normalized;
    }
}
