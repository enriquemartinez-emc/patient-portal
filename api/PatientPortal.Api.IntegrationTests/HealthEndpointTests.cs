using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PatientPortal.Api.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Health_returns_ok()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Default", "Host=localhost;Database=unused")
        );
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
