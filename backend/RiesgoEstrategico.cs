using System.Globalization;

namespace Sisplan.Api;

internal sealed record RiesgoInput(string? Nombre, string? Descripcion, int? Probabilidad, int? Impacto,
    string? Tratamiento, string? AccionMitigacion, string? Responsable, string? FechaRevision,
    string? EvidenciaUrl, bool? Cerrado);

internal sealed record RiesgoDto(Guid Id, Guid ObjetivoId, string Nombre, string Descripcion,
    int Probabilidad, int Impacto, string? Tratamiento, string? AccionMitigacion, string? Responsable,
    DateOnly? FechaRevision, string? EvidenciaUrl, bool Cerrado, long Revision = 1)
{
    public int Puntuacion => Probabilidad * Impacto;
    public string Nivel => Puntuacion switch { <= 4 => "bajo", <= 14 => "medio", _ => "alto" };
    public string ETag() => $"\"riesgo-{Id:N}-{Revision.ToString(CultureInfo.InvariantCulture)}\"";
}

internal enum RiesgoWriteStatus { Saved, NotFound, ReadOnlyPei, InactiveStructure, Stale }
internal sealed record RiesgoWriteResult(RiesgoWriteStatus Status, RiesgoDto? Riesgo = null);

internal static class RiesgoValidation
{
    public static (RiesgoDto? Riesgo, Dictionary<string, string[]> Errors) Validate(
        RiesgoInput input, Guid objetivoId, Guid id)
    {
        var errors = new Dictionary<string, string[]>();
        var nombre = Text(input.Nombre, "nombre", 3, 200, errors, required: true);
        var descripcion = Text(input.Descripcion, "descripcion", 1, 4000, errors, required: true);
        var tratamiento = Text(input.Tratamiento, "tratamiento", 1, 200, errors);
        var accion = Text(input.AccionMitigacion, "accionMitigacion", 1, 4000, errors);
        var responsable = Text(input.Responsable, "responsable", 1, 200, errors);
        var evidencia = Text(input.EvidenciaUrl, "evidenciaUrl", 1, 2048, errors);
        var fecha = input.FechaRevision?.Trim();
        var probabilidad = input.Probabilidad.GetValueOrDefault();
        var impacto = input.Impacto.GetValueOrDefault();
        var cerrado = input.Cerrado ?? false;

        if (probabilidad is < 1 or > 5)
            errors["probabilidad"] = ["Indica una probabilidad de 1 a 5."];
        if (impacto is < 1 or > 5)
            errors["impacto"] = ["Indica un impacto de 1 a 5."];

        DateOnly? fechaRevision = null;
        if (!string.IsNullOrEmpty(fecha))
        {
            if (DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var valid))
                fechaRevision = valid;
            else
                errors["fechaRevision"] = ["Indica una fecha de revisión válida en formato AAAA-MM-DD."];
        }
        if (evidencia is not null &&
            (!evidencia.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
             !Uri.TryCreate(evidencia, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
             string.IsNullOrWhiteSpace(uri.Host) || uri.UserInfo.Length != 0 || evidencia.Any(char.IsWhiteSpace)))
            errors["evidenciaUrl"] = ["Indica un enlace HTTPS absoluto sin credenciales."];

        if (cerrado && probabilidad is >= 1 and <= 5 && impacto is >= 1 and <= 5 && probabilidad * impacto >= 15)
        {
            RequireMitigation(tratamiento is not null, "tratamiento", errors);
            RequireMitigation(accion is not null, "accionMitigacion", errors);
            RequireMitigation(responsable is not null, "responsable", errors);
            RequireMitigation(fechaRevision.HasValue, "fechaRevision", errors);
            RequireMitigation(evidencia is not null, "evidenciaUrl", errors);
        }

        return (errors.Count == 0
            ? new(id, objetivoId, nombre!, descripcion!, probabilidad, impacto, tratamiento, accion,
                responsable, fechaRevision, evidencia, cerrado)
            : null, errors);
    }

    private static void RequireMitigation(bool present, string field, Dictionary<string, string[]> errors)
    {
        if (!present)
            errors.TryAdd(field, ["Completa este campo para cerrar un riesgo alto."]);
    }

    private static string? Text(string? input, string field, int min, int max,
        Dictionary<string, string[]> errors, bool required = false)
    {
        var value = input?.Trim() ?? "";
        if (value.Length == 0 && !required)
            return null;
        if (value.Length < min || value.Length > max || value.Contains('\0'))
            errors[field] = [$"Indica un texto entre {min} y {max} caracteres válidos."];
        return value;
    }
}
