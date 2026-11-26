using System.Text.Json;

namespace Sisplan.Api;

internal static class RiesgoEstrategicoEndpoints
{
    public static void MapRiesgosEstrategicos(this RouteGroupBuilder objetivos)
    {
        var riesgos = objetivos.MapGroup("/{objetivoId:guid}/riesgos");
        riesgos.MapGet("", ListAsync);
        riesgos.MapGet("/{riesgoId:guid}", FindAsync);
        riesgos.MapPost("", CreateAsync).RequireAuthorization("EditarPei");
        riesgos.MapPut("/{riesgoId:guid}", UpdateAsync).RequireAuthorization("EditarPei");
    }

    private static async Task<IResult> ListAsync(Guid peiId, Guid ejeId, Guid objetivoId,
        RiesgoEstrategicoRepository repository, CancellationToken ct)
    {
        var riesgos = await repository.ListAsync(peiId, ejeId, objetivoId, ct);
        return riesgos is null ? Missing() : Results.Ok(riesgos);
    }

    private static async Task<IResult> FindAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid riesgoId,
        HttpResponse response, RiesgoEstrategicoRepository repository, CancellationToken ct)
    {
        var riesgo = await repository.FindAsync(peiId, ejeId, objetivoId, riesgoId, ct);
        if (riesgo is null)
            return Missing();
        response.Headers.ETag = riesgo.ETag();
        return Results.Ok(riesgo);
    }

    private static Task<IResult> CreateAsync(Guid peiId, Guid ejeId, Guid objetivoId, HttpRequest request,
        HttpResponse response, RiesgoEstrategicoRepository repository, CancellationToken ct) =>
        SaveAsync(peiId, ejeId, objetivoId, Guid.NewGuid(), null, request, response, repository, ct);

    private static Task<IResult> UpdateAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid riesgoId,
        HttpRequest request, HttpResponse response, RiesgoEstrategicoRepository repository, CancellationToken ct)
    {
        var tag = request.Headers.IfMatch.ToString();
        return string.IsNullOrWhiteSpace(tag)
            ? Task.FromResult(Results.Problem(statusCode: 428, title: "Envía el ETag vigente del riesgo en If-Match."))
            : SaveAsync(peiId, ejeId, objetivoId, riesgoId, tag, request, response, repository, ct);
    }

    private static async Task<IResult> SaveAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid riesgoId, string? expectedTag,
        HttpRequest request, HttpResponse response, RiesgoEstrategicoRepository repository, CancellationToken ct)
    {
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía los campos del riesgo como JSON.");
        RiesgoInput? input;
        try
        {
            input = await request.ReadFromJsonAsync<RiesgoInput>(ct);
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "El cuerpo JSON del riesgo no es válido.");
        }
        if (input is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Envía los campos del riesgo."] }, statusCode: 422);
        var (fields, errors) = RiesgoValidation.Validate(input, objetivoId, riesgoId);
        if (fields is null)
            return Results.ValidationProblem(errors, statusCode: 422, title: "Revisa los campos del riesgo.");

        var result = await repository.WriteAsync(peiId, ejeId, fields, expectedTag, ct);
        if (result.Riesgo is { } saved)
        {
            response.Headers.ETag = saved.ETag();
            return expectedTag is null
                ? Results.Created($"/api/v1/peis/{peiId:D}/ejes/{ejeId:D}/objetivos/{objetivoId:D}/riesgos/{riesgoId:D}", saved)
                : Results.Ok(saved);
        }
        return result.Status switch
        {
            RiesgoWriteStatus.NotFound => Missing(),
            RiesgoWriteStatus.Stale => Results.Problem(statusCode: 412, title: "El riesgo cambió. Carga la revisión actual."),
            RiesgoWriteStatus.ReadOnlyPei => Results.Problem(statusCode: 409, title: "El PEI no está disponible para edición."),
            RiesgoWriteStatus.InactiveStructure => Results.Problem(statusCode: 409, title: "El eje o el objetivo está inactivo."),
            _ => throw new InvalidOperationException("Resultado inesperado al guardar el riesgo.")
        };
    }

    private static IResult Missing() => Results.Problem(statusCode: 404, title: "No se encontró el objetivo o el riesgo en este PEI.");
}
