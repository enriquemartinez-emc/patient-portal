using System.Net;
using System.Net.Http.Json;
using PatientPortal.Api.Features.Organizations;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class OrganizationsEndpointTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Lists_organizations_by_name_with_their_kind()
    {
        var prefix = $"Test-{Guid.NewGuid():N}";
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var second = await SchemaData.InsertNamedOrganizationAsync(owner, $"{prefix} B Clinic");
        var research = await SchemaData.InsertOrganizationAsync(owner, "research_institution");
        var first = await SchemaData.InsertNamedOrganizationAsync(owner, $"{prefix} A Clinic");

        var response = await _api.Client.GetAsync("/organizations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListOrganizationsResponse>();
        var mine = body!.Items.Where(item => item.Name.StartsWith(prefix)).ToList();
        Assert.Equal([first, second], mine.Select(item => item.Id));
        Assert.All(mine, item => Assert.Equal("clinic", item.Kind));
        Assert.Contains(
            body.Items,
            item => item.Id == research && item.Kind == "research_institution"
        );
    }
}
