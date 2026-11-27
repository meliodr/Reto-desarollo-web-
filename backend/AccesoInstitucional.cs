using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Sisplan.Api;

internal sealed record AccesoInstitucional(string Url, string Realm, string ClientId, string Audience)
{
    public string Authority => $"{Url}/realms/{Realm}";

    public static AccesoInstitucional From(IConfiguration configuration)
    {
        var section = configuration.GetSection("Sisplan:Oidc");
        var url = section["Url"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Configura Sisplan__Oidc__Url para autenticar la API.");

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

    public static void Configure(WebApplicationBuilder builder, AccesoInstitucional oidc)
    {
        var authentication = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);
        authentication.AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
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
            .AddPolicy("LeerPlanes", policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => context.Resource is HttpContext http
                    && http.RequestServices.GetRequiredService<SesionInstitucional>().Activa is not null))
            .AddPolicy("EditarPei", policy => Roles(policy, "AnalistaPlanificacion", "Planificador"))
            .AddPolicy("FormularPoa", policy => Roles(policy, "AnalistaPlanificacion", "Planificador", "DirectorPlanificacion", "ResponsableArea"))
            .AddPolicy("ConsultarAuditoria", policy => Roles(policy, "Auditor", "AdministradorInstitucional"))
            .AddPolicy("TransitarPei", policy => Roles(policy, "AnalistaPlanificacion", "Planificador", "DirectorPlanificacion", "ComiteEstrategico", "MaximaAutoridad"));
    }

    private static void Roles(Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder policy, params string[] roles)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.Resource is HttpContext http
            && http.RequestServices.GetRequiredService<SesionInstitucional>().Activa is { } institucion
            && roles.Contains(institucion.Rol, StringComparer.Ordinal));
    }
}
