using System.Security.Claims;
using System.Text.Json;

namespace Sisplan.Api;

internal static class FlujoPeiEndpoints
{
    public static void MapFlujoPei(this RouteGroupBuilder api)
    {
        var transitions = api.MapGroup("/pei-versiones/{versionId:guid}/transiciones")
            .RequireAuthorization("Planificador");
        transitions.MapGet("", OptionsAsync);
        transitions.MapPost("", ApplyAsync);
        api.MapGet("/peis/{peiId:guid}/poas", ListPoasAsync).RequireAuthorization("Planificador");
    }

    private static async Task<IResult> OptionsAsync(Guid versionId, ClaimsPrincipal user,
        FlujoPeiRepository repository, CancellationToken ct)
    {
        if (Actor(user) is not { } actor)
            return Results.Forbid();
        var options = await repository.OptionsAsync(versionId, actor, ct);
        return options is null ? MissingPei() : Results.Ok(options);
    }

    private static async Task<IResult> ApplyAsync(Guid versionId, ClaimsPrincipal user, HttpRequest request,
        HttpResponse response, FlujoPeiRepository repository, CancellationToken ct)
    {
        if (Actor(user) is not { } actor)
            return Results.Forbid();

        var expectedTag = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(expectedTag))
            return Results.Problem(statusCode: 428, title: "Envía el ETag vigente del PEI en If-Match.");
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía la transición como JSON.");

        FlujoPeiInput? input;
        try
        {
            input = await request.ReadFromJsonAsync<FlujoPeiInput>(ct);
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "El cuerpo JSON de la transición no es válido.");
        }

        var action = input?.Accion?.Trim();
        var reason = input?.Motivo?.Trim();
        var errors = new Dictionary<string, string[]>();
        if (action is not ("enviar" or "aprobar" or "devolver" or "publicar"))
            errors["accion"] = ["Selecciona una acción de transición válida."];
        if (action == "devolver" && string.IsNullOrEmpty(reason))
            errors["motivo"] = ["Indica el motivo para devolver el PEI a borrador."];
        else if (reason is { Length: > 4000 } || reason?.Contains('\0') == true)
            errors["motivo"] = ["Usa hasta 4000 caracteres válidos."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors, statusCode: 422);

        var result = await repository.ApplyAsync(versionId, expectedTag, action!,
            string.IsNullOrEmpty(reason) ? null : reason, actor, ct);
        if (result.Snapshot is { } saved)
        {
            response.Headers.ETag = saved.ETag;
            return Results.Ok(saved.Pei);
        }

        return result.Status switch
        {
            FlujoPeiStatus.NotFound => MissingPei(),
            FlujoPeiStatus.Stale => Results.Problem(statusCode: 412, title: "El PEI cambió. Carga su versión actual."),
            FlujoPeiStatus.Conflict => Results.Problem(statusCode: 409, title: "La acción no está disponible en el estado actual del PEI."),
            FlujoPeiStatus.SelfApproval => Results.Problem(statusCode: 403, title: "La aprobación requiere un planificador distinto de quien envió el PEI."),
            FlujoPeiStatus.IncompleteStructure => Results.Problem(statusCode: 409, title: "El PEI no tiene su estructura estratégica completa.",
                detail: "Incluye al menos un eje activo, con objetivos e indicadores para cada objetivo."),
            FlujoPeiStatus.OverlappingPeriod => Results.Problem(statusCode: 409, title: "El período coincide con otro PEI publicado."),
            _ => throw new InvalidOperationException("Resultado de transición de PEI inesperado.")
        };
    }

    private static async Task<IResult> ListPoasAsync(Guid peiId, FlujoPeiRepository repository, CancellationToken ct)
    {
        var poas = await repository.ListPoasAsync(peiId, ct);
        return poas is null ? MissingPei() : Results.Ok(poas);
    }

    private static FlujoPeiActor? Actor(ClaimsPrincipal user)
    {
        var issuer = user.FindFirstValue("iss");
        var subject = user.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject) ? null : new(issuer, subject);
    }

    private static IResult MissingPei() => Results.Problem(statusCode: 404, title: "No se encontró el PEI vigente para esta operación.");
}
