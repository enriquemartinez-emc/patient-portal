using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PatientPortal.Api.Infrastructure.Auth;

public static class AuthenticationSetup
{
    // The API is a stateless resource server: it only validates the bearer token that the web app
    // forwards. Keycloak signs the tokens; login, sessions and refresh live in the web app.
    public static IServiceCollection AddPortalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.GetSection("Authentication");
        var metadataAddress = Required(section, "MetadataAddress");
        var validIssuer = Required(section, "ValidIssuer");
        var audience = Required(section, "Audience");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Keycloak is reached at one address inside the network and another from the
                // browser, so the signing keys come from the first while the issuer is the second.
                options.MetadataAddress = metadataAddress;
                options.RequireHttpsMetadata = section.GetValue("RequireHttpsMetadata", true);
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = validIssuer,
                    ValidAudience = audience,
                    NameClaimType = "preferred_username",
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        AddRealmRoles(context.Principal);
                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }

    private static string Required(IConfigurationSection section, string key) =>
        section[key]
        ?? throw new InvalidOperationException($"Authentication:{key} is not configured.");

    private static void AddRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (realmAccess is null)
        {
            return;
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (
            document.RootElement.TryGetProperty("roles", out var roles)
            && roles.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var role in roles.EnumerateArray())
            {
                if (role.GetString() is { } name)
                {
                    identity.AddClaim(new Claim(identity.RoleClaimType, name));
                }
            }
        }
    }
}
