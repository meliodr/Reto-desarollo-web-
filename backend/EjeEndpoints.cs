using System.Text.Json;

namespace Sisplan.Api;

internal static class EjeEndpoints
{
    public static void MapEjes(this RouteGroupBuilder api)
    {
        var ejes = api.MapGroup("/peis/{peiId:guid}/ejes");
        ejes.MapGet("", ListAsync);
        ejes.MapPost("", CreateAsync);
        ejes.MapGet("/{ejeId:guid}", FindAsync);
        ejes.MapPut("/{ejeId:guid}", UpdateAsync);
        ejes.MapGet("/{ejeId:guid}/versiones", HistoryAsync);
        ejes.MapObjetivos();
    }

    private static async Task<IResult> ListAsync(Guid peiId, EjeRepository repository, CancellationToken cancellationToken)
    {
        var ejes = await repository.ListAsync(peiId, cancellationToken);
        return ejes is null ? MissingPei() : Results.Ok(ejes);
    }

    private static async Task<IResult> FindAsync(Guid peiId, Guid ejeId, HttpResponse response,
        EjeRepository repository, CancellationToken cancellationToken)
    {
        var eje = await repository.FindAsync(peiId, ejeId, cancellationToken);
        if (eje is null)
            return MissingEje();

        response.Headers.ETag = eje.ETag();
        return Results.Ok(eje);
    }

    private static async Task<IResult> HistoryAsync(Guid peiId, Guid ejeId, EjeRepository repository,
        CancellationToken cancellationToken)
    {
        var versions = await repository.HistoryAsync(peiId, ejeId, cancellationToken);
        return versions is null ? MissingEje() : Results.Ok(versions);
    }

    private static async Task<IResult> CreateAsync(Guid peiId, HttpRequest request, HttpResponse response,
        EjeRepository repository, CancellationToken cancellationToken)
    {
        var (input, error) = await ReadAsync<EjeInput>(request, cancellationToken);
        if (error is not null)
            return error;

        var errors = new Dictionary<string, string[]>();
        var fields = ValidateFields(input!.Nombre, input.Descripcion, errors);
        if (errors.Count > 0)
            return InvalidFields(errors);

        var result = await repository.CreateAsync(peiId, fields, cancellationToken);
        if (result.Status == EjeWriteStatus.NotFound)
            return MissingPei();
        if (result.Status == EjeWriteStatus.Conflict)
            return ReadOnlyPei();

        var saved = result.Eje!;
        response.Headers.ETag = saved.ETag();
        return Results.Created($"/api/v1/peis/{peiId:D}/ejes/{saved.Id:D}", saved);
    }

    private static async Task<IResult> UpdateAsync(Guid peiId, Guid ejeId, HttpRequest request, HttpResponse response,
        EjeRepository repository, CancellationToken cancellationToken)
    {
        var expected = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(expected))
            return Results.Problem(statusCode: 428, title: "Envía el ETag vigente del eje en If-Match.");

        var (input, error) = await ReadAsync<EjeUpdateInput>(request, cancellationToken);
        if (error is not null)
            return error;

        var errors = new Dictionary<string, string[]>();
        var fields = ValidateFields(input!.Nombre, input.Descripcion, errors);
        if (input.Activo is null)
            errors["activo"] = ["Indica si el eje está activo."];
        if (input.Revision is null or <= 0)
            errors["revision"] = ["Indica la revisión vigente del eje."];
        if (errors.Count > 0)
            return InvalidFields(errors);

        var revision = input.Revision!.Value;
        if (!string.Equals(expected, EjeDto.ETagFor(ejeId, revision), StringComparison.Ordinal))
            return StaleVersion();

        var result = await repository.UpdateAsync(peiId, ejeId, revision, fields, input.Activo!.Value, cancellationToken);
        if (result.Status == EjeWriteStatus.NotFound)
            return MissingEje();
        if (result.Status == EjeWriteStatus.Conflict)
            return ReadOnlyPei();
        if (result.Status == EjeWriteStatus.Stale)
            return StaleVersion();

        var saved = result.Eje!;
        response.Headers.ETag = saved.ETag();
        return Results.Ok(saved);
    }

    private static EjeFields ValidateFields(string? name, string? description, Dictionary<string, string[]> errors)
    {
        var normalizedName = name?.Trim() ?? "";
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedName.Length is < 3 or > 200 || normalizedName.Contains('\0'))
            errors["nombre"] = ["Indica un nombre válido de entre 3 y 200 caracteres."];
        if (normalizedDescription is { Length: > 4000 } || normalizedDescription?.Contains('\0') == true)
            errors["descripcion"] = ["Indica una descripción válida de hasta 4000 caracteres."];
        return new EjeFields(normalizedName, normalizedDescription);
    }

    private static async Task<(T? Input, IResult? Error)> ReadAsync<T>(HttpRequest request,
        CancellationToken cancellationToken) where T : class
    {
        if (!request.HasJsonContentType())
            return (null, Results.Problem(statusCode: 415, title: "Envía el eje como JSON."));

        try
        {
            var input = await request.ReadFromJsonAsync<T>(cancellationToken);
            return input is null
                ? (null, InvalidFields(new Dictionary<string, string[]> { ["body"] = ["Envía los campos del eje."] }))
                : (input, null);
        }
        catch (JsonException)
        {
            return (null, Results.Problem(statusCode: 400, title: "El cuerpo JSON del eje no es válido."));
        }
    }

    private static IResult InvalidFields(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, statusCode: 422, title: "Revisa los campos del eje.");

    private static IResult MissingPei() => Results.Problem(statusCode: 404, title: "No se encontró el PEI.");

    private static IResult MissingEje() => Results.Problem(statusCode: 404, title: "No se encontró el eje en la versión actual del PEI.");

    private static IResult ReadOnlyPei() => Results.Problem(statusCode: 409, title: "Solo se pueden cambiar los ejes de un PEI en borrador.");

    private static IResult StaleVersion() => Results.Problem(statusCode: 412, title: "El eje cambió. Consulta la revisión actual antes de guardar.");
}
