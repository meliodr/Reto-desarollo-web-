using System.Globalization;
using System.Text.Json;

namespace Sisplan.Api;

internal static class FlujoPeiEndpoints
{
    public static void MapFlujoPei(this RouteGroupBuilder api)
    {
        var transitions = api.MapGroup("/pei-versiones/{versionId:guid}/transiciones")
            .RequireAuthorization("LeerPlanes");
        transitions.MapGet("", OptionsAsync);
        transitions.MapPost("", ApplyAsync).RequireAuthorization("TransitarPei");
        api.MapGet("/peis/{peiId:guid}/versiones", ListVersionsAsync);
        api.MapPost("/peis/{peiId:guid}/versiones", CreateRevisionAsync).RequireAuthorization("EditarPei");
        api.MapGet("/pei-versiones/{versionId:guid}", ReadVersionAsync);
        api.MapGet("/pei-versiones/{versionId:guid}/historial", HistoryAsync);
        api.MapGet("/peis/{peiId:guid}/poas", ListPoasAsync).WithMetadata(new RequiereModulo(Pei: true, Poa: true));
    }

    private static async Task<IResult> OptionsAsync(Guid versionId, SesionInstitucional sesion,
        FlujoPeiRepository repository, CancellationToken ct)
    {
        var actor = Actor(sesion);
        var options = await repository.OptionsAsync(versionId, actor, ct);
        return options is null ? MissingPei() : Results.Ok(options);
    }

    private static async Task<IResult> ApplyAsync(Guid versionId, SesionInstitucional sesion, HttpRequest request,
        HttpResponse response, FlujoPeiRepository repository, CancellationToken ct)
    {
        var actor = Actor(sesion);

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
        if (action is not ("enviar" or "revisar" or "validar_comite" or "aprobar" or "devolver" or "corregir" or "publicar"))
            errors["accion"] = ["Selecciona una acción de transición válida."];
        if (action == "devolver" && string.IsNullOrEmpty(reason))
            errors["motivo"] = ["Indica el motivo para observar el PEI."];
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
            FlujoPeiStatus.Forbidden => Results.Problem(statusCode: 403, title: "Tu rol o participación en este PEI no autoriza la acción."),
            FlujoPeiStatus.IncompleteStructure => Results.Problem(statusCode: 409, title: "El PEI no tiene su estructura estratégica completa.",
                detail: "Incluye al menos un eje activo. Cada eje activo debe tener un objetivo activo y cada objetivo activo debe tener un indicador vinculado."),
            FlujoPeiStatus.PendingManualIndicators => Results.Problem(statusCode: 409,
                title: "Hay indicadores manuales pendientes de configuración.",
                detail: "Devuelve el PEI para completar la fórmula, unidad, frecuencia, sentido, método y meta de cada indicador manual aplicable. Los indicadores de DIGEMAPS pueden quedar pendientes."),
            FlujoPeiStatus.OverlappingPeriod => Results.Problem(statusCode: 409, title: "El período coincide con otro PEI publicado."),
            _ => throw new InvalidOperationException("Resultado de transición de PEI inesperado.")
        };
    }

    private static async Task<IResult> ListVersionsAsync(Guid peiId, string? fecha, PeiRepository repository,
        CancellationToken ct)
    {
        DateOnly? period = null;
        if (fecha is not null)
        {
            if (!DateOnly.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                return Results.Problem(statusCode: 400, title: "Indica la fecha en formato AAAA-MM-DD.");
            period = day;
        }
        var versions = await repository.ListVersionsAsync(peiId, period, ct);
        return versions is null ? MissingPei() : Results.Ok(versions);
    }

    private static async Task<IResult> CreateRevisionAsync(Guid peiId, HttpRequest request, HttpResponse response,
        PeiRepository repository, CancellationToken ct)
    {
        var expected = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(expected))
            return Results.Problem(statusCode: 428, title: "Envía el ETag vigente de la versión oficial en If-Match.");
        if (!request.HasJsonContentType())
            return Results.Problem(statusCode: 415, title: "Envía la revisión como JSON.");

        PeiRevisionInput? input;
        try
        {
            input = await request.ReadFromJsonAsync<PeiRevisionInput>(ct);
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "El cuerpo JSON de la revisión no es válido.");
        }

        var reason = input?.Motivo?.Trim();
        if (reason is null or { Length: 0 or > 4000 } || reason.Contains('\0'))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["motivo"] = ["Indica un motivo válido de hasta 4000 caracteres."],
            }, statusCode: 422);

        var result = await repository.CreateRevisionAsync(peiId, expected, reason, ct);
        if (result.Snapshot is { } saved)
        {
            response.Headers.ETag = saved.ETag;
            return Results.Created($"/api/v1/pei-versiones/{saved.Pei.VersionId:D}", saved.Pei);
        }

        return result.Status switch
        {
            PeiRevisionStatus.NotFound => MissingPei(),
            PeiRevisionStatus.Stale => Results.Problem(statusCode: 412, title: "El PEI cambió. Carga su versión oficial actual."),
            PeiRevisionStatus.NotPublished => Results.Problem(statusCode: 409,
                title: "Publica la versión en curso antes de abrir otra revisión."),
            _ => throw new InvalidOperationException("Resultado de revisión de PEI inesperado.")
        };
    }

    private static async Task<IResult> ReadVersionAsync(Guid versionId, HttpResponse response,
        PeiRepository repository, CancellationToken ct)
    {
        var snapshot = await repository.FindVersionAsync(versionId, ct);
        if (snapshot is null)
            return MissingPei();
        response.Headers.ETag = snapshot.ETag;
        return Results.Ok(snapshot.Pei);
    }

    private static async Task<IResult> HistoryAsync(Guid versionId, FlujoPeiRepository repository, CancellationToken ct)
    {
        var history = await repository.HistoryAsync(versionId, ct);
        return history is null ? MissingPei() : Results.Ok(history);
    }

    private static async Task<IResult> ListPoasAsync(Guid peiId, FlujoPeiRepository repository, CancellationToken ct)
    {
        var poas = await repository.ListPoasAsync(peiId, ct);
        return poas is null ? MissingPei() : Results.Ok(poas);
    }

    private static FlujoPeiActor Actor(SesionInstitucional sesion)
    {
        var rol = sesion.Activa!.Rol;
        return new(sesion.Emisor, sesion.Sujeto, rol is "AnalistaPlanificacion" or "Planificador",
            rol == "DirectorPlanificacion", rol == "ComiteEstrategico", rol == "MaximaAutoridad");
    }

    private static IResult MissingPei() => Results.Problem(statusCode: 404, title: "No se encontró el PEI vigente para esta operación.");
}
