using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PatientPortal.Api.IntegrationTests;

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
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
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
            });
        });
        Client = _factory.CreateClient();
    }

    public FixedTimeProvider Time { get; }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
    }
}
