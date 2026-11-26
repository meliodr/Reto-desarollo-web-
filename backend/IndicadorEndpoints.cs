using System.Globalization;
using System.Text.Json;

namespace Sisplan.Api;

internal static class IndicadorEndpoints
{
    public static void MapIndicadores(this RouteGroupBuilder objetivos)
    {
        var indicadores = objetivos.MapGroup("/{objetivoId:guid}/indicadores");
        indicadores.MapGet("", ListAsync);
        indicadores.MapPost("", CreateAsync).RequireAuthorization("EditarPei");
        indicadores.MapGet("/{indicadorId:guid}", FindAsync);
        indicadores.MapPut("/{indicadorId:guid}", UpdateAsync).RequireAuthorization("EditarPei");
        indicadores.MapGet("/{indicadorId:guid}/mediciones", ListMeasurementsAsync);
        indicadores.MapPost("/{indicadorId:guid}/mediciones", CreateMeasurementAsync).RequireAuthorization("EditarPei");
    }

    private static async Task<IResult> FindAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid indicadorId,
        HttpResponse response, IndicadorRepository repository, CancellationToken cancellationToken)
    {
        var indicator = await repository.FindAsync(peiId, ejeId, objetivoId, indicadorId, cancellationToken);
        if (indicator is null)
            return MissingIndicator();

        response.Headers.ETag = indicator.ETag();
        return Results.Ok(indicator);
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

    private static async Task<IResult> CreateAsync(Guid peiId, Guid ejeId, Guid objetivoId, HttpRequest request, HttpResponse response,
        IndicadorRepository repository, CancellationToken cancellationToken)
    {
        var (input, error) = await ReadAsync<IndicadorInput>(request, cancellationToken);
        if (error is not null)
            return error;

        var errors = new Dictionary<string, string[]>();
        var fields = ValidateFields(Guid.NewGuid(), input!, errors);
        if (errors.Count > 0)
            return InvalidFields(errors);

        var result = await repository.CreateAsync(peiId, ejeId, objetivoId, fields, cancellationToken);
        if (result == IndicadorWriteStatus.Saved)
        {
            response.Headers.ETag = fields.ETag();
            return Results.Created($"/api/v1/peis/{peiId:D}/ejes/{ejeId:D}/objetivos/{objetivoId:D}/indicadores/{fields.Id:D}", fields);
        }
        return result switch
        {
            IndicadorWriteStatus.NotFound => MissingObjective(),
            IndicadorWriteStatus.ReadOnlyPei => ReadOnlyPei(),
            IndicadorWriteStatus.InactiveAxis => InactiveAxis(),
            IndicadorWriteStatus.InactiveObjective => InactiveObjective(),
            _ => throw new InvalidOperationException("Resultado inesperado al crear un indicador."),
        };
    }

    private static async Task<IResult> UpdateAsync(Guid peiId, Guid ejeId, Guid objetivoId, Guid indicadorId,
        HttpRequest request, HttpResponse response, IndicadorRepository repository, CancellationToken cancellationToken)
    {
        var expected = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(expected))
            return Results.Problem(statusCode: 428, title: "Envía el ETag vigente del indicador en If-Match.");

        var (input, error) = await ReadAsync<IndicadorInput>(request, cancellationToken);
        if (error is not null)
            return error;

        var errors = new Dictionary<string, string[]>();
        var fields = ValidateFields(indicadorId, input!, errors);
        if (errors.Count > 0)
            return InvalidFields(errors);

        var (status, saved) = await repository.UpdateAsync(peiId, ejeId, objetivoId, fields, expected, cancellationToken);
        if (status == IndicadorWriteStatus.Saved)
        {
            response.Headers.ETag = saved!.ETag();
            return Results.Ok(saved);
        }
        return status switch
        {
            IndicadorWriteStatus.NotFound => MissingIndicator(),
            IndicadorWriteStatus.ReadOnlyPei => ReadOnlyPei(),
            IndicadorWriteStatus.InactiveAxis => InactiveAxis(),
            IndicadorWriteStatus.InactiveObjective => InactiveObjective(),
            IndicadorWriteStatus.Stale => Results.Problem(statusCode: 412, title: "El indicador cambió. Consulta la revisión actual antes de guardar."),
            _ => throw new InvalidOperationException("Resultado inesperado al configurar un indicador."),
        };
    }

    private static IndicadorDto ValidateFields(Guid id, IndicadorInput input, Dictionary<string, string[]> errors)
    {
        var sentido = input.Sentido?.Trim();
        if (sentido is not ("ascendente" or "descendente"))
            errors["sentido"] = ["Selecciona un sentido ascendente o descendente."];

        var metodo = input.MetodoCalculo?.Trim();
        if (metodo is not ("valor_directo" or "variacion_absoluta" or "variacion_porcentual"))
            errors["metodoCalculo"] = ["Selecciona un método de cálculo válido."];

        var lineaBase = input.LineaBase is not null || metodo is "variacion_absoluta" or "variacion_porcentual"
            ? RequiredDecimal(input.LineaBase, "lineaBase", errors)
            : null;
        if (metodo == "variacion_porcentual" && lineaBase == 0)
            errors["lineaBase"] = ["Para la variación porcentual indica una línea base distinta de cero."];

        return new IndicadorDto(id,
            RequiredText(input.Nombre, "nombre", 3, 200, errors),
            RequiredText(input.Formula, "formula", 1, 4000, errors),
            RequiredText(input.Unidad, "unidad", 1, 100, errors),
            lineaBase,
            RequiredDecimal(input.Meta, "meta", errors),
            RequiredText(input.Frecuencia, "frecuencia", 1, 100, errors),
            sentido, metodo, errors.Count == 0, Revision: 1);
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

        var data = new IndicadorMedicionDto(Guid.NewGuid(), input.Periodo!.Value, value!.Value);
        var result = await repository.CreateMeasurementAsync(peiId, ejeId, objetivoId, indicadorId, data, cancellationToken);
        return result switch
        {
            IndicadorWriteStatus.Saved => Results.Json(data, statusCode: 201),
            IndicadorWriteStatus.NotFound => MissingIndicator(),
            IndicadorWriteStatus.ReadOnlyPei => ReadOnlyPei(),
            IndicadorWriteStatus.InactiveAxis => InactiveAxis(),
            IndicadorWriteStatus.InactiveObjective => InactiveObjective(),
            IndicadorWriteStatus.PendingConfiguration => Results.Problem(statusCode: 409, title: "Completa la configuración del indicador antes de registrar mediciones."),
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

    private static decimal? RequiredDecimal(JsonElement? input, string field, Dictionary<string, string[]> errors)
    {
        const decimal max = 999999999999.9999m;
        var raw = input?.ValueKind switch
        {
            JsonValueKind.Number => input.Value.GetRawText(),
            JsonValueKind.String => input.Value.GetString(),
            _ => null,
        };
        var text = raw.AsSpan().Trim();
        if (!text.Contains('\0') && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && value is >= -max and <= max && FitsDecimalScale(text))
            return value;

        errors[field] = ["Indica un número con hasta 12 cifras enteras y 4 decimales."];
        return null;
    }

    private static bool FitsDecimalScale(ReadOnlySpan<char> raw)
    {
        // decimal.TryParse puede redondear durante la conversión. Medir la escala del valor original.
        var exponentStart = raw.IndexOfAny('e', 'E');
        var mantissa = exponentStart >= 0 ? raw[..exponentStart] : raw;
        var lastNonZero = mantissa.LastIndexOfAnyInRange('1', '9');
        if (lastNonZero < 0)
            return true;
        var exponent = 0;
        if (exponentStart >= 0 && !int.TryParse(raw[(exponentStart + 1)..], NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out exponent))
            return false;

        var point = mantissa.IndexOf('.');
        var scale = point < 0 ? lastNonZero - (mantissa.Length - 1)
            : lastNonZero - point + (lastNonZero < point ? 1 : 0);
        return scale - (long)exponent <= 4;
    }

    private static IResult InvalidFields(Dictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, statusCode: 422, title: "Revisa los campos del registro.");

    private static IResult MissingObjective() =>
        Results.Problem(statusCode: 404, title: "No se encontró el objetivo en el eje de este PEI.");

    private static IResult MissingIndicator() =>
        Results.Problem(statusCode: 404, title: "No se encontró el indicador en el objetivo de este PEI.");

    private static IResult ReadOnlyPei() =>
        Results.Problem(statusCode: 409, title: "El PEI no está disponible para editar su estructura.");

    private static IResult InactiveAxis() =>
        Results.Problem(statusCode: 409, title: "El eje está inactivo.");

    private static IResult InactiveObjective() =>
        Results.Problem(statusCode: 409, title: "El objetivo está inactivo.");
}
