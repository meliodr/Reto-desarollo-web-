using System.Text.Json;

namespace Sisplan.Api;

internal static class ObjetivoEndpoints
{
    public static void MapObjetivos(this RouteGroupBuilder ejes)
    {
        var objetivos = ejes.MapGroup("/{ejeId:guid}/objetivos");
        objetivos.MapGet("", ListAsync);
        objetivos.MapPost("", CreateAsync).RequireAuthorization("EditarPei");
        objetivos.MapGet("/{objetivoId:guid}", FindAsync);
        objetivos.MapPut("/{objetivoId:guid}", UpdateAsync).RequireAuthorization("EditarPei");
        objetivos.MapPatch("/{objetivoId:guid}/estado", SetActiveAsync).RequireAuthorization("EditarPei");
        objetivos.MapIndicadores();
        objetivos.MapRiesgosEstrategicos();
    }

    private static async Task<IResult> ListAsync(Guid peiId, Guid ejeId, ObjetivoRepository repository,
        CancellationToken cancellationToken)
    {
        var objetivos = await repository.ListAsync(peiId, ejeId, cancellationToken);
        return objetivos is null ? MissingEje() : Results.Ok(objetivos);
    }

    private static async Task<IResult> FindAsync(Guid peiId, Guid ejeId, Guid objetivoId, HttpResponse response,
        ObjetivoRepository repository, CancellationToken cancellationToken)
    {
        var objetivo = await repository.FindAsync(peiId, ejeId, objetivoId, cancellationToken);
        if (objetivo is null)
            return MissingObjective();
        response.Headers.ETag = objetivo.ETag();
        return Results.Ok(objetivo);
    }

    private static async Task<IResult> SetActiveAsync(Guid peiId, Guid ejeId, Guid objetivoId, HttpRequest request,
        HttpResponse response, ObjetivoRepository repository, CancellationToken ct)
    {
        var expectedTag = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(expectedTag))
            return MissingTag();
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía el estado del objetivo como JSON.");

        ObjetivoEstadoInput? input;
        try
        {
            input = await request.ReadFromJsonAsync<ObjetivoEstadoInput>(ct);
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "El cuerpo JSON del estado no es válido.");
        }
        if (input?.Activo is not bool active)
            return InvalidFields(new() { ["activo"] = ["Indica si el objetivo está activo."] });

        var result = await repository.SetActiveAsync(peiId, ejeId, objetivoId, expectedTag, active, ct);
        return UpdateResult(result, response);
    }

    private static async Task<IResult> UpdateAsync(Guid peiId, Guid ejeId, Guid objetivoId, HttpRequest request,
        HttpResponse response, ObjetivoRepository repository, CancellationToken ct)
    {
        var expectedTag = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(expectedTag))
            return MissingTag();

        var (fields, error) = await ReadFieldsAsync(objetivoId, request, ct);
        if (error is not null)
            return error;

        var result = await repository.UpdateAsync(peiId, ejeId, fields!, expectedTag, ct);
        return UpdateResult(result, response);
    }

    private static IResult UpdateResult(ObjetivoUpdateResult result, HttpResponse response)
    {
        if (result.Objetivo is { } saved)
        {
            response.Headers.ETag = saved.ETag();
            return Results.Ok(saved);
        }
        return result.Status switch
        {
            ObjetivoUpdateStatus.NotFound => MissingObjective(),
            ObjetivoUpdateStatus.Stale => Results.Problem(statusCode: 412, title: "El objetivo cambió. Carga la revisión actual."),
            ObjetivoUpdateStatus.ReadOnlyPei => Results.Problem(statusCode: 409, title: "El PEI no está disponible para edición."),
            ObjetivoUpdateStatus.InactiveAxis => Results.Problem(statusCode: 409, title: "Activa el eje antes de cambiar el objetivo."),
            ObjetivoUpdateStatus.InactiveObjective => Results.Problem(statusCode: 409, title: "Activa el objetivo antes de cambiar su contenido."),
            ObjetivoUpdateStatus.DuplicateCode => DuplicateCode(),
            _ => throw new InvalidOperationException("Resultado inesperado al actualizar el objetivo.")
        };
    }

    private static async Task<IResult> CreateAsync(Guid peiId, Guid ejeId, HttpRequest request, HttpResponse response,
        ObjetivoRepository repository, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var (fields, error) = await ReadFieldsAsync(id, request, cancellationToken);
        if (error is not null)
            return error;

        var result = await repository.CreateAsync(peiId, ejeId, fields!, cancellationToken);
        if (result.Objetivo is { } saved)
            response.Headers.ETag = saved.ETag();
        return result.Status switch
        {
            ObjetivoCreateStatus.Saved => Results.Created(
                $"/api/v1/peis/{peiId:D}/ejes/{ejeId:D}/objetivos/{id:D}", result.Objetivo),
            ObjetivoCreateStatus.NotFound => MissingEje(),
            ObjetivoCreateStatus.ReadOnlyPei => Results.Problem(statusCode: 409, title: "El PEI no está disponible para edición."),
            ObjetivoCreateStatus.InactiveAxis => Results.Problem(statusCode: 409, title: "El eje está inactivo."),
            ObjetivoCreateStatus.DuplicateCode => DuplicateCode(),
            _ => throw new InvalidOperationException("Resultado inesperado al crear un objetivo.")
        };
    }

    private static async Task<(ObjetivoDto? Fields, IResult? Error)> ReadFieldsAsync(Guid id, HttpRequest request,
        CancellationToken ct)
    {
        if (!request.HasJsonContentType())
            return (null, Results.Problem(statusCode: 415, title: "Envía el objetivo como JSON."));

        ObjetivoInput? input;
        try
        {
            input = await request.ReadFromJsonAsync<ObjetivoInput>(ct);
        }
        catch (JsonException)
        {
            return (null, Results.Problem(statusCode: 400, title: "El cuerpo JSON del objetivo no es válido."));
        }

        if (input is null)
            return (null, InvalidFields(new() { ["body"] = ["Envía los campos del objetivo."] }));

        var errors = new Dictionary<string, string[]>();
        var fields = new ObjetivoDto(id,
            RequiredText(input.Codigo, "codigo", "código", 1, 50, errors),
            RequiredText(input.Nombre, "nombre", "nombre", 3, 200, errors),
            RequiredText(input.Descripcion, "descripcion", "descripción", 1, 4000, errors),
            RequiredText(input.ResultadoEsperado, "resultadoEsperado", "resultado esperado", 1, 4000, errors),
            RequiredText(input.LineaAccion, "lineaAccion", "línea de acción", 1, 4000, errors));
        return errors.Count > 0 ? (null, InvalidFields(errors)) : (fields, null);
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

    private static IResult MissingEje() => Results.Problem(statusCode: 404, title: "No se encontró el eje solicitado en este PEI.");

    private static IResult MissingObjective() => Results.Problem(statusCode: 404, title: "No se encontró el objetivo en el eje de este PEI.");

    private static IResult MissingTag() => Results.Problem(statusCode: 428, title: "Envía el ETag vigente del objetivo en If-Match.");

    private static IResult DuplicateCode() => Results.Problem(statusCode: 409, title: "Ya existe un objetivo con ese código en este eje.");
}
