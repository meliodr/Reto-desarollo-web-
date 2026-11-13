using System.Text.Json;

namespace Sisplan.Api;

internal static class IndicadorEndpoints
{
    public static void MapIndicadores(this RouteGroupBuilder objetivos)
    {
        var indicadores = objetivos.MapGroup("/{objetivoId:guid}/indicadores");
        indicadores.MapGet("", ListAsync);
        indicadores.MapPost("", CreateAsync);
        indicadores.MapGet("/{indicadorId:guid}/mediciones", ListMeasurementsAsync);
        indicadores.MapPost("/{indicadorId:guid}/mediciones", CreateMeasurementAsync);
    }

    private static async Task<IResult> ListAsync(Guid peiId, Guid ejeId, Guid objetivoId, IndicadorRepository repository,
        CancellationToken cancellationToken)
    {
        var result = await repository.ListAsync(peiId, ejeId, objetivoId, cancellationToken);
        return result is null ? MissingObjective() : Results.Ok(result);
    }

    private static async Task<IResult> ListMeasurementsAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid indicadorId,
        IndicadorRepository repository, CancellationToken cancellationToken)
    {
        var result = await repository.ListMeasurementsAsync(peiId, ejeId, objetivoId, indicadorId, cancellationToken);
        return result is null ? MissingIndicator() : Results.Ok(result);
    }

    private static async Task<IResult> CreateAsync(Guid peiId, Guid ejeId, Guid objetivoId, HttpRequest request,
        IndicadorRepository repository, CancellationToken cancellationToken)
    {
        var (input, error) = await ReadAsync<IndicadorInput>(request, cancellationToken);
        if (error is not null)
            return error;

        var errors = new Dictionary<string, string[]>();
        var fields = new IndicadorDto(Guid.NewGuid(),
            RequiredText(input!.Nombre, "nombre", 3, 200, errors),
            RequiredText(input.Formula, "formula", 1, 4000, errors),
            RequiredText(input.Unidad, "unidad", 1, 100, errors),
            RequiredDecimal(input.LineaBase, "lineaBase", errors),
            RequiredDecimal(input.Meta, "meta", errors),
            RequiredText(input.Frecuencia, "frecuencia", 1, 100, errors));
        if (errors.Count > 0)
            return InvalidFields(errors);

        var result = await repository.CreateAsync(peiId, ejeId, objetivoId, fields, cancellationToken);
        return result switch
        {
            IndicadorWriteStatus.Saved => Results.Json(fields, statusCode: 201),
            IndicadorWriteStatus.NotFound => MissingObjective(),
            IndicadorWriteStatus.ReadOnlyPei => ReadOnlyPei(),
            IndicadorWriteStatus.InactiveAxis => InactiveAxis(),
            _ => throw new InvalidOperationException("Resultado inesperado al crear un indicador."),
        };
    }

    private static async Task<IResult> CreateMeasurementAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid indicadorId,
        HttpRequest request, IndicadorRepository repository, CancellationToken cancellationToken)
    {
        var (input, error) = await ReadAsync<IndicadorMedicionInput>(request, cancellationToken);
        if (error is not null)
            return error;

        var errors = new Dictionary<string, string[]>();
        if (input!.Periodo is null)
            errors["periodo"] = ["Indica una fecha para el período."];
        var value = RequiredDecimal(input.Valor, "valor", errors);
        if (errors.Count > 0)
            return InvalidFields(errors);

        var data = new IndicadorMedicionDto(Guid.NewGuid(), input.Periodo!.Value, value);
        var result = await repository.CreateMeasurementAsync(peiId, ejeId, objetivoId, indicadorId, data, cancellationToken);
        return result switch
        {
            IndicadorWriteStatus.Saved => Results.Json(data, statusCode: 201),
            IndicadorWriteStatus.NotFound => MissingIndicator(),
            IndicadorWriteStatus.ReadOnlyPei => ReadOnlyPei(),
            IndicadorWriteStatus.InactiveAxis => InactiveAxis(),
            IndicadorWriteStatus.OutsidePlan => InvalidFields(new() { ["periodo"] = ["La fecha debe estar dentro del período del PEI."] }),
            IndicadorWriteStatus.DuplicatePeriod => Results.Problem(statusCode: 409, title: "El indicador ya tiene una medición para esa fecha."),
            _ => throw new InvalidOperationException("Resultado inesperado al registrar una medición."),
        };
    }

    private static async Task<(T? Input, IResult? Error)> ReadAsync<T>(HttpRequest request,
        CancellationToken cancellationToken) where T : class
    {
        if (!request.HasJsonContentType())
            return (null, Results.Problem(statusCode: 415, title: "Envía los datos como JSON."));

        try
        {
            var input = await request.ReadFromJsonAsync<T>(cancellationToken);
            return input is null
                ? (null, InvalidFields(new() { ["body"] = ["Envía los campos del registro."] }))
                : (input, null);
        }
        catch (JsonException)
        {
            return (null, Results.Problem(statusCode: 400, title: "El cuerpo JSON no es válido."));
        }
    }

    private static string RequiredText(string? value, string field, int min, int max, Dictionary<string, string[]> errors)
    {
        var text = value?.Trim() ?? "";
        if (text.Length < min || text.Length > max || text.Contains('\0'))
            errors[field] = [$"Indica entre {min} y {max} caracteres válidos."];
        return text;
    }

    private static decimal RequiredDecimal(decimal? value, string field, Dictionary<string, string[]> errors)
    {
        const decimal max = 999999999999.9999m;
        if (value is null || value < -max || value > max || decimal.Round(value.Value, 4) != value)
            errors[field] = ["Indica un número con hasta 12 cifras enteras y 4 decimales."];
        return value.GetValueOrDefault();
    }

    private static IResult InvalidFields(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, statusCode: 422, title: "Revisa los campos del registro.");

    private static IResult MissingObjective() =>
        Results.Problem(statusCode: 404, title: "No se encontró el objetivo en el eje de este PEI.");

    private static IResult MissingIndicator() =>
        Results.Problem(statusCode: 404, title: "No se encontró el indicador en el objetivo de este PEI.");

    private static IResult ReadOnlyPei() =>
        Results.Problem(statusCode: 409, title: "El PEI no está en borrador.");

    private static IResult InactiveAxis() =>
        Results.Problem(statusCode: 409, title: "El eje está inactivo.");
}
