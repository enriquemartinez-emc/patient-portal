using System.Net;
using System.Net.Http.Json;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ResearcherLabResultsTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private sealed record World(
        Guid Org,
        Guid Researcher,
        Guid Patient,
        Guid Lipids,
        Guid Hematology,
        Guid Urinalysis
    );

    private async Task<World> ArrangeAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner, "research_institution");
        var researcher = await SchemaData.InsertResearcherAsync(owner, org);
        var patient = await SchemaData.InsertPatientAsync(owner);
        var at = _api.Time.Now.AddDays(-2);
        return new World(
            org,
            researcher,
            patient,
            await SchemaData.InsertLabResultAsync(owner, patient, "lipids", "LDL", at),
            await SchemaData.InsertLabResultAsync(
                owner,
                patient,
                "hematology",
                "Hemoglobin",
                at.AddHours(1)
            ),
            await SchemaData.InsertLabResultAsync(
                owner,
                patient,
                "urinalysis",
                "Protein",
                at.AddHours(2)
            )
        );
    }

    private Task<HttpResponseMessage> ReadAsync(World w) =>
        _api.Client.GetAsync($"/researchers/{w.Researcher}/patients/{w.Patient}/lab-results");

    private static async Task<List<Guid>> IdsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListLabResultsResponse>();
        return [.. body!.Items.Select(item => item.Id)];
    }

    // A refusal returns no data and is recorded in the patient's audit trail as an access_denied entry.
    private async Task AssertRefusedAndRecordedAsync(HttpResponseMessage response, World w)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var entry = Assert.Single(await SchemaData.AuditReadsAsync(owner, w.Patient));
        Assert.Equal("researcher", entry.ActorKind);
        Assert.Equal(w.Researcher, entry.ActorId);
        Assert.Equal("access_denied", entry.Action);
        Assert.Empty(entry.LabResultIds);
    }

    [Fact]
    public async Task A_researcher_sees_only_the_consented_categories_and_the_read_is_audited()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            _api.Time.Now.AddDays(-1),
            categories: ["hematology"]
        );

        var ids = await IdsAsync(await ReadAsync(w));

        Assert.Equal([w.Hematology], ids);
        var entry = Assert.Single(await SchemaData.AuditReadsAsync(owner, w.Patient));
        Assert.Equal("researcher", entry.ActorKind);
        Assert.Equal(w.Researcher, entry.ActorId);
        Assert.Equal([w.Hematology], entry.LabResultIds);
        Assert.Equal(_api.Time.Now, entry.OccurredAt);
    }

    [Fact]
    public async Task Several_consents_combine_and_revoked_or_expired_ones_are_ignored()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var now = _api.Time.Now;
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            now.AddDays(-3),
            categories: ["lipids"]
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            now.AddDays(-3),
            expiresAt: now.AddDays(1),
            categories: ["urinalysis"]
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            now.AddDays(-3),
            revokedAt: now.AddDays(-1),
            categories: ["hematology"]
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            now.AddDays(-3),
            expiresAt: now,
            categories: ["hematology"]
        );

        var ids = await IdsAsync(await ReadAsync(w));

        Assert.Equal([w.Urinalysis, w.Lipids], ids);
    }

    [Fact]
    public async Task Without_a_consent_access_is_refused_and_recorded()
    {
        var w = await ArrangeAsync();

        await AssertRefusedAndRecordedAsync(await ReadAsync(w), w);
    }

    [Fact]
    public async Task A_consent_to_another_organization_does_not_grant_access()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var otherOrg = await SchemaData.InsertOrganizationAsync(owner);
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            otherOrg,
            _api.Time.Now.AddDays(-1)
        );

        await AssertRefusedAndRecordedAsync(await ReadAsync(w), w);
    }

    [Fact]
    public async Task Someone_elses_researcher_id_or_a_missing_patient_is_forbidden_and_records_nothing()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertConsentAtAsync(owner, w.Patient, w.Org, _api.Time.Now.AddDays(-1));
        var impostor = _api.ClientWith(
            TestTokens.Create(Guid.NewGuid().ToString(), ["researcher"])
        );

        var impersonated = await impostor.GetAsync(
            $"/researchers/{w.Researcher}/patients/{w.Patient}/lab-results"
        );
        var missingPatient = await _api.Client.GetAsync(
            $"/researchers/{w.Researcher}/patients/{Guid.NewGuid()}/lab-results"
        );

        Assert.Equal(HttpStatusCode.Forbidden, impersonated.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, missingPatient.StatusCode);
        Assert.Empty(await SchemaData.AuditReadsAsync(owner, w.Patient));
    }
}
