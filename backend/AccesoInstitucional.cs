using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Sisplan.Api;

internal sealed record AccesoInstitucional(string Url, string Realm, string ClientId, string Audience)
{
    public string Authority => $"{Url}/realms/{Realm}";

    public static AccesoInstitucional? From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Sisplan:Oidc");
        var url = section["Url"];
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("La URL de OIDC requiere HTTPS o HTTP en loopback.");

        var realm = section["Realm"] ?? "sisplan";
        var clientId = section["ClientId"] ?? "sisplan-web";
        var audience = section["Audience"] ?? "sisplan-api";
        if (string.IsNullOrWhiteSpace(realm) || realm.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_'))
            || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Configura el realm, el cliente y la audiencia de SISPLAN.");

        return new AccesoInstitucional(url.TrimEnd('/'), realm, clientId, audience);
    }

    public static void Configure(WebApplicationBuilder builder, AccesoInstitucional? oidc)
    {
        var authentication = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        authentication.AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            if (oidc is null)
            {
                // El entorno local sin proveedor no autentica solicitudes Bearer.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context => { context.NoResult(); return Task.CompletedTask; }
                };
                return;
            }

            options.Authority = oidc.Authority;
            options.Audience = oidc.Audience;
            options.RequireHttpsMetadata = new Uri(oidc.Url).Scheme == "https";
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username",
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidTypes = ["JWT", "at+jwt"],
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("LeerPlanes", policy => SetupPolicy(policy, oidc, PlanningRoles))
            .AddPolicy("EditarPei", policy => SetupPolicy(policy, oidc, ["AnalistaPlanificacion", "Planificador"]))
            .AddPolicy("FormularPoa", policy => SetupPolicy(policy, oidc,
                ["AnalistaPlanificacion", "Planificador", "DirectorPlanificacion", "ResponsableArea"]));
    }

    private static readonly string[] PlanningRoles =
        ["AnalistaPlanificacion", "Planificador", "DirectorPlanificacion", "ComiteEstrategico",
         "MaximaAutoridad", "AdministradorInstitucional", "ResponsableArea", "DirectorArea", "Auditor"];

    private static void SetupPolicy(Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder policy,
        AccesoInstitucional? oidc, string[] roles)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("iss");
        policy.RequireClaim("sub");
        policy.RequireAssertion(context => oidc is not null
            && roles.Any(role => HasRole(context.User, oidc.ClientId, role)));
    }

    internal static bool HasRole(System.Security.Claims.ClaimsPrincipal user, string clientId, string role) =>
        user.FindAll("resource_access").Any(claim => HasClientRole(claim.Value, clientId, role));

    private static bool HasClientRole(string value, string clientId, string expectedRole)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(clientId, out var client)
                && client.ValueKind == JsonValueKind.Object
                && client.TryGetProperty("roles", out var roles)
                && roles.ValueKind == JsonValueKind.Array
                && roles.EnumerateArray().Any(role => role.ValueKind == JsonValueKind.String && role.GetString() == expectedRole);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
