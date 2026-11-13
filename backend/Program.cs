using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Npgsql;
using Sisplan.Api;

var migrate = args.Length == 1 && args[0] == "--migrate";
if (args.Length != 0 && !migrate)
{
    Console.Error.WriteLine("Uso: Sisplan.Api [--migrate]");
    return 2;
}

var builder = WebApplication.CreateBuilder(Array.Empty<string>());
if (!builder.Environment.IsDevelopment())
{
    Console.Error.WriteLine("Este incremento local requiere ASPNETCORE_ENVIRONMENT=Development.");
    return 1;
}

var connectionString = builder.Configuration.GetConnectionString("Sisplan");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Configura ConnectionStrings__Sisplan con la conexión PostgreSQL del entorno local.");
    return 1;
}

// Conserva las fechas de DateOnly como fechas literales de PostgreSQL.
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

if (migrate)
{
    try
    {
        await using var migrationSource = NpgsqlDataSource.Create(connectionString);
        await SchemaMigrator.ApplyAsync(migrationSource);
    }
    catch (NpgsqlException exception)
    {
        var reason = exception is PostgresException postgres ? $" PostgreSQL devolvió {postgres.SqlState}." : "";
        Console.Error.WriteLine($"No se pudieron aplicar las migraciones de SISPLAN.{reason} Revisa la conexión y los permisos de PostgreSQL.");
        return 1;
    }
    Console.WriteLine("Migraciones de SISPLAN aplicadas.");
    return 0;
}

var port = builder.Configuration.GetValue("Sisplan:Puerto", 5080);
if (port is < 1 or > 65535)
{
    Console.Error.WriteLine("Sisplan__Puerto debe indicar un puerto TCP entre 1 y 65535.");
    return 1;
}

builder.WebHost.PreferHostingUrls(false);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Configure(new ConfigurationBuilder().Build(), reloadOnChange: false);
    options.Listen(IPAddress.Loopback, port, endpoint => endpoint.Protocols = HttpProtocols.Http1);
    options.Limits.MaxRequestBodySize = 128 * 1024;
});
var oidc = AccesoInstitucional.From(builder.Configuration);
AccesoInstitucional.Configure(builder, oidc);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddSingleton<PeiRepository>();
builder.Services.AddSingleton<EjeRepository>();
builder.Services.AddSingleton<ObjetivoRepository>();
builder.Services.AddSingleton<IndicadorRepository>();
builder.Services.AddSingleton<FlujoPeiRepository>();

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    var remote = context.Connection.RemoteIpAddress;
    var local = context.Connection.LocalIpAddress;
    var origin = context.Request.Headers.Origin.ToString();
    if (remote is null || local is null || !IPAddress.IsLoopback(remote) || !IPAddress.IsLoopback(local)
        || !IsLocalHost(context.Request.Host.Host) || !IsLocalOrigin(origin))
    {
        await Results.Problem(statusCode: 403, title: "La API de borradores solo está disponible en el entorno local.").ExecuteAsync(context);
        return;
    }

    try
    {
        await next(context);
    }
    catch (NpgsqlException) when (!context.Response.HasStarted)
    {
        app.Logger.LogWarning("No se pudo completar una operación PostgreSQL de SISPLAN.");
        await Results.Problem(statusCode: 503, title: "La base de datos de SISPLAN no está disponible.").ExecuteAsync(context);
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/configuracion", () => Results.Ok(new
{
    Oidc = oidc is null ? null : new { oidc.Url, oidc.Realm, oidc.ClientId },
}));
var api = app.MapGroup("/api/v1");
if (oidc is not null)
    api.RequireAuthorization("Planificador");
api.MapEjes();
api.MapFlujoPei();
api.MapGet("/peis", async (PeiRepository repository, CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListAsync(cancellationToken)));

api.MapGet("/peis/{id:guid}", async (Guid id, HttpResponse response, PeiRepository repository, CancellationToken cancellationToken) =>
{
    var snapshot = await repository.FindAsync(id, cancellationToken);
    if (snapshot is null)
        return Results.Problem(statusCode: 404, title: "No se encontró el PEI.");
    response.Headers.ETag = snapshot.ETag;
    return Results.Ok(snapshot.Pei);
});

api.MapPost("/peis", async (HttpRequest request, HttpResponse response, PeiRepository repository, CancellationToken cancellationToken) =>
{
    var (fields, error) = await ReadFieldsAsync(request, cancellationToken);
    if (error is not null)
        return error;

    var snapshot = await repository.CreateAsync(fields!, cancellationToken);
    response.Headers.ETag = snapshot.ETag;
    return Results.Created($"/api/v1/peis/{snapshot.Pei.Id:D}", snapshot.Pei);
});

api.MapPatch("/pei-versiones/{versionId:guid}", async (Guid versionId, HttpRequest request, HttpResponse response,
    PeiRepository repository, CancellationToken cancellationToken) =>
{
    var expected = request.Headers.IfMatch.ToString();
    if (string.IsNullOrWhiteSpace(expected))
        return Results.Problem(statusCode: 428, title: "Envía el ETag vigente del borrador en If-Match.");

    var current = await repository.FindVersionAsync(versionId, cancellationToken);
    if (current is null)
        return Results.Problem(statusCode: 404, title: "No se encontró la versión del PEI.");
    if (!string.Equals(expected, current.ETag, StringComparison.Ordinal))
        return StaleVersion();

    if (current.Pei.Estado != "borrador")
        return Results.Problem(statusCode: 409, title: "Solo se pueden editar borradores de PEI.");

    var (fields, error) = await ReadFieldsAsync(request, cancellationToken);
    if (error is not null)
        return error;

    PeiSnapshot? saved;
    try
    {
        saved = await repository.UpdateAsync(versionId, current.Revision, fields!, cancellationToken);
    }
    catch (PeriodoConMedicionesException exception)
    {
        var errors = new Dictionary<string, string[]>();
        if (exception.Inicio)
            errors["fechaInicio"] = ["El inicio debe incluir las mediciones ya registradas."];
        if (exception.Fin)
            errors["fechaFin"] = ["El fin debe incluir las mediciones ya registradas."];
        return Results.ValidationProblem(errors, statusCode: 422);
    }
    if (saved is null)
        return StaleVersion();
    response.Headers.ETag = saved.ETag;
    return Results.Ok(saved.Pei);
});

await app.RunAsync();
return 0;

static bool IsLocalHost(string host) =>
    string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
    || (IPAddress.TryParse(host.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));

static bool IsLocalOrigin(string origin) =>
    string.IsNullOrEmpty(origin)
    || (Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https" && IsLocalHost(uri.Host));

static IResult StaleVersion() =>
    Results.Problem(statusCode: 412, title: "El borrador cambió. Carga la versión actual antes de guardar.");

static async Task<(PeiFields? Fields, IResult? Error)> ReadFieldsAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (!request.HasJsonContentType())
        return (null, Results.Problem(statusCode: 415, title: "Envía el borrador como JSON."));

    PeiInput? input;
    try
    {
        input = await request.ReadFromJsonAsync<PeiInput>(cancellationToken);
    }
    catch (JsonException)
    {
        return (null, Results.Problem(statusCode: 400, title: "El cuerpo JSON del borrador no es válido."));
    }

    if (input is null)
        return (null, Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Envía los campos del borrador."] }, statusCode: 422));

    var (fields, errors) = PeiValidation.Validate(input);
    return fields is null
        ? (null, Results.ValidationProblem(errors, statusCode: 422, title: "Revisa los campos del borrador."))
        : (fields, null);
}
