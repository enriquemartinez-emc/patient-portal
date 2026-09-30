using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace PatientPortal.Api.IntegrationTests;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

// Tokens signed with a key the test host trusts instead of Keycloak's.
public static class TestTokens
{
    public const string Issuer = "http://localhost:8080/realms/patient-portal";
    public const string Audience = "portal-api";

    public static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("integration-tests-signing-key-32-bytes-long!")
    );

    public static string Create(
        string subject,
        string[]? roles,
        string issuer = Issuer,
        string audience = Audience,
        SecurityKey? key = null,
        TimeSpan? lifetime = null
    )
    {
        var life = lifetime ?? TimeSpan.FromHours(1);
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = now.AddDays(-1),
            NotBefore = now.AddDays(-1),
            Expires = now.Add(life),
            SigningCredentials = new SigningCredentials(key ?? Key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["preferred_username"] = $"{subject}@demo.example",
            },
        };
        if (roles is not null)
        {
            descriptor.Claims["realm_access"] = new Dictionary<string, object>
            {
                ["roles"] = roles,
            };
        }
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

// Hosts the real API against the shared Postgres container, connecting as the application role.
public sealed class ApiHarness : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiHarness(PostgresFixture postgres)
    {
        var now = DateTimeOffset.UtcNow;
        Time = new FixedTimeProvider(
            new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                now.Second,
                TimeSpan.Zero
            )
        );
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", postgres.AppConnectionString);
            builder.UseSetting(
                "Authentication:MetadataAddress",
                "http://keycloak.invalid/openid-configuration"
            );
            builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
            builder.UseSetting("Authentication:ValidIssuer", TestTokens.Issuer);
            builder.UseSetting("Authentication:Audience", TestTokens.Audience);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
                services.PostConfigure<JwtBearerOptions>(
                    JwtBearerDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.ConfigurationManager =
                            new StaticConfigurationManager<OpenIdConnectConfiguration>(
                                new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer }
                            );
                        options.TokenValidationParameters.IssuerSigningKey = TestTokens.Key;
                    }
                );
            });
        });
        Client = _factory.CreateDefaultClient(new ActAsRouteHandler());
    }

    public FixedTimeProvider Time { get; }

    // Signed in as the person named in the URL (patients/{id}, clinicians/{id}, researchers/{id}),
    // an admin for treatment relationships and a patient for organizations. Tests that care who
    // is calling use AnonymousClient or ClientWith instead.
    public HttpClient Client { get; }

    public HttpClient AnonymousClient => _factory.CreateClient();

    public HttpClient ClientWith(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
    }

    private sealed class ActAsRouteHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (
                request.Headers.Authorization is null
                && request.RequestUri is { } uri
                && TokenFor(uri.AbsolutePath) is { } token
            )
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return base.SendAsync(request, cancellationToken);
        }

        private static string? TokenFor(string path) =>
            path.Split('/', StringSplitOptions.RemoveEmptyEntries) switch
            {
                ["patients", var id, ..] => TestTokens.Create(id, ["patient"]),
                ["clinicians", var id, ..] => TestTokens.Create(id, ["clinician"]),
                ["researchers", var id, ..] => TestTokens.Create(id, ["researcher"]),
                ["treatment-relationships", ..] => TestTokens.Create(
                    Guid.NewGuid().ToString(),
                    ["admin"]
                ),
                ["organizations", ..] => TestTokens.Create(Guid.NewGuid().ToString(), ["patient"]),
                _ => null,
            };
    }
}
