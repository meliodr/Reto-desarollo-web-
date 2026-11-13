using System.Text.Json;

namespace Sisplan.Api;

internal static class ObjetivoEndpoints
{
    public static void MapObjetivos(this RouteGroupBuilder ejes)
    {
        var objetivos = ejes.MapGroup("/{ejeId:guid}/objetivos");
        objetivos.MapGet("", ListAsync);
        objetivos.MapPost("", CreateAsync);
        objetivos.MapGet("/{objetivoId:guid}", FindAsync);
        objetivos.MapIndicadores();
    }

    private static async Task<IResult> ListAsync(Guid peiId, Guid ejeId, ObjetivoRepository repository,
        CancellationToken cancellationToken)
    {
        var objetivos = await repository.ListAsync(peiId, ejeId, cancellationToken);
        return objetivos is null ? MissingEje() : Results.Ok(objetivos);
    }

    private static async Task<IResult> FindAsync(Guid peiId, Guid ejeId, Guid objetivoId,
        ObjetivoRepository repository, CancellationToken cancellationToken)
    {
        var objetivo = await repository.FindAsync(peiId, ejeId, objetivoId, cancellationToken);
        return objetivo is null
            ? Results.Problem(statusCode: 404, title: "No se encontró el objetivo en el eje de este PEI.")
            : Results.Ok(objetivo);
    }

    private static async Task<IResult> CreateAsync(Guid peiId, Guid ejeId, HttpRequest request,
        ObjetivoRepository repository, CancellationToken cancellationToken)
    {
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía el objetivo como JSON.");

        ObjetivoInput? input;
        try
        {
            input = await request.ReadFromJsonAsync<ObjetivoInput>(cancellationToken);
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "El cuerpo JSON del objetivo no es válido.");
        }

        if (input is null)
            return InvalidFields(new Dictionary<string, string[]> { ["body"] = ["Envía los campos del objetivo."] });

        var errors = new Dictionary<string, string[]>();
        var fields = new ObjetivoDto(Guid.NewGuid(),
            RequiredText(input.Codigo, "codigo", "código", 1, 50, errors),
            RequiredText(input.Nombre, "nombre", "nombre", 3, 200, errors),
            RequiredText(input.Descripcion, "descripcion", "descripción", 1, 4000, errors),
            RequiredText(input.ResultadoEsperado, "resultadoEsperado", "resultado esperado", 1, 4000, errors),
            RequiredText(input.LineaAccion, "lineaAccion", "línea de acción", 1, 4000, errors));
        if (errors.Count > 0)
            return InvalidFields(errors);

        var result = await repository.CreateAsync(peiId, ejeId, fields, cancellationToken);
        return result.Status switch
        {
            ObjetivoCreateStatus.Saved => Results.Created(
                $"/api/v1/peis/{peiId:D}/ejes/{ejeId:D}/objetivos/{fields.Id:D}", result.Objetivo),
            ObjetivoCreateStatus.NotFound => MissingEje(),
            ObjetivoCreateStatus.ReadOnlyPei => Results.Problem(statusCode: 409, title: "El PEI no está en borrador."),
            ObjetivoCreateStatus.InactiveAxis => Results.Problem(statusCode: 409, title: "El eje está inactivo."),
            ObjetivoCreateStatus.DuplicateCode => Results.Problem(statusCode: 409, title: "Ya existe un objetivo con ese código en este eje."),
            _ => throw new InvalidOperationException("Resultado inesperado al crear un objetivo.")
        };
    }

    private static string RequiredText(string? value, string key, string label, int min, int max,
        Dictionary<string, string[]> errors)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < min || text.Length > max || text.Contains('\0'))
            errors[key] = [$"El campo {label} debe contener entre {min} y {max} caracteres válidos."];
        return text;
    }

    private static IResult InvalidFields(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, statusCode: 422, title: "Revisa los campos del objetivo.");

    private static IResult MissingEje() => Results.Problem(statusCode: 404, title: "No se encontró el eje en la versión actual del PEI.");
}
