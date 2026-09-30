using System.Net;
using System.Net.Http.Json;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Researchers;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ResearchParticipantsTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private async Task<PagedResponse<ResearchParticipantResponse>> GetAsync(
        Guid researcher,
        string query = ""
    )
    {
        var response = await _api.Client.GetAsync($"/researchers/{researcher}/patients{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (
            await response.Content.ReadFromJsonAsync<PagedResponse<ResearchParticipantResponse>>()
        )!;
    }

    [Fact]
    public async Task Lists_patients_with_an_in_effect_consent_and_the_categories_they_cover()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner, "research_institution");
        var otherOrg = await SchemaData.InsertOrganizationAsync(owner);
        var researcher = await SchemaData.InsertResearcherAsync(owner, org);
        var now = _api.Time.Now;
        var granted = now.AddDays(-5);

        var combined = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(
            owner,
            combined,
            org,
            granted,
            categories: ["lipids", "hematology"]
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            combined,
            org,
            granted,
            expiresAt: now.AddDays(1),
            categories: ["hematology", "urinalysis"]
        );
        var revoked = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(
            owner,
            revoked,
            org,
            granted,
            revokedAt: now.AddDays(-1)
        );
        var expiredNow = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(owner, expiredNow, org, granted, expiresAt: now);
        var elsewhere = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(owner, elsewhere, otherOrg, granted);

        var page = await GetAsync(researcher);

        var participant = Assert.Single(page.Items);
        Assert.Equal(combined, participant.PatientId);
        Assert.Equal(["hematology", "lipids", "urinalysis"], participant.Categories);
    }

    [Fact]
    public async Task Participants_are_paged()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner, "research_institution");
        var researcher = await SchemaData.InsertResearcherAsync(owner, org);
        for (var i = 0; i < 3; i++)
        {
            var patient = await SchemaData.InsertPatientAsync(owner);
            await SchemaData.InsertConsentAtAsync(owner, patient, org, _api.Time.Now.AddDays(-1));
        }

        var first = await GetAsync(researcher, "?pageSize=2");
        var second = await GetAsync(researcher, "?page=2&pageSize=2");

        Assert.Equal([2, 1], [first.Items.Count, second.Items.Count]);
        Assert.Equal([true, false], [first.HasNextPage, second.HasNextPage]);
        Assert.Equal(
            3,
            first.Items.Concat(second.Items).Select(item => item.PatientId).Distinct().Count()
        );
    }

    [Fact]
    public async Task A_researcher_without_consents_gets_an_empty_page()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var researcher = await SchemaData.InsertResearcherAsync(
            owner,
            await SchemaData.InsertOrganizationAsync(owner, "research_institution")
        );

        var page = await GetAsync(researcher);

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task An_unknown_researcher_is_not_found()
    {
        var response = await _api.Client.GetAsync($"/researchers/{Guid.NewGuid()}/patients");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    public async Task Invalid_paging_is_rejected(string query)
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var researcher = await SchemaData.InsertResearcherAsync(
            owner,
            await SchemaData.InsertOrganizationAsync(owner, "research_institution")
        );

        var response = await _api.Client.GetAsync($"/researchers/{researcher}/patients{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
