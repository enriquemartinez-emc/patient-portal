using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ClinicianLabResultsTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private sealed record World(
        Guid Org,
        Guid Clinician,
        Guid Patient,
        Guid Lipids,
        Guid Hematology,
        Guid Urinalysis
    );

    private async Task<World> ArrangeAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        var patient = await SchemaData.InsertPatientAsync(owner);
        var other = await SchemaData.InsertPatientAsync(owner);
        var at = _api.Time.Now.AddDays(-2);
        await SchemaData.InsertLabResultAsync(owner, other, "lipids", "Someone else's", at);
        return new World(
            org,
            clinician,
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
        _api.Client.GetAsync($"/clinicians/{w.Clinician}/patients/{w.Patient}/lab-results");

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
        Assert.DoesNotContain("testName", await response.Content.ReadAsStringAsync());
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var entry = Assert.Single(await SchemaData.AuditReadsAsync(owner, w.Patient));
        Assert.Equal("clinician", entry.ActorKind);
        Assert.Equal(w.Clinician, entry.ActorId);
        Assert.Equal("access_denied", entry.Action);
        Assert.Empty(entry.LabResultIds);
        Assert.Equal(_api.Time.Now, entry.OccurredAt);
    }

    [Fact]
    public async Task A_treating_clinician_sees_every_category_and_the_read_is_audited()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertTreatmentAsync(
            owner,
            w.Patient,
            w.Clinician,
            _api.Time.Now.AddDays(-10)
        );

        var ids = await IdsAsync(await ReadAsync(w));

        Assert.Equal([w.Urinalysis, w.Hematology, w.Lipids], ids);
        var entry = Assert.Single(await SchemaData.AuditReadsAsync(owner, w.Patient));
        Assert.Equal("clinician", entry.ActorKind);
        Assert.Equal(w.Clinician, entry.ActorId);
        Assert.Equal("lab_results_read", entry.Action);
        Assert.Equal(ids, entry.LabResultIds);
        Assert.Equal(_api.Time.Now, entry.OccurredAt);
    }

    [Fact]
    public async Task A_clinician_with_only_a_consent_sees_just_the_consented_categories()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            _api.Time.Now.AddDays(-1),
            categories: ["lipids", "hematology"]
        );

        var ids = await IdsAsync(await ReadAsync(w));

        Assert.Equal([w.Hematology, w.Lipids], ids);
        var entry = Assert.Single(await SchemaData.AuditReadsAsync(owner, w.Patient));
        Assert.Equal(ids, entry.LabResultIds);
    }

    [Fact]
    public async Task Several_consents_combine_their_categories()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var granted = _api.Time.Now.AddDays(-1);
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            granted,
            categories: ["lipids"]
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            granted,
            categories: ["urinalysis"]
        );

        var ids = await IdsAsync(await ReadAsync(w));

        Assert.Equal([w.Urinalysis, w.Lipids], ids);
    }

    [Fact]
    public async Task Treatment_takes_precedence_over_a_narrower_consent()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertTreatmentAsync(
            owner,
            w.Patient,
            w.Clinician,
            _api.Time.Now.AddDays(-10)
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            _api.Time.Now.AddDays(-1),
            categories: ["lipids"]
        );

        var ids = await IdsAsync(await ReadAsync(w));

        Assert.Equal(3, ids.Count);
    }

    [Fact]
    public async Task Without_treatment_or_consent_access_is_refused_and_recorded()
    {
        var w = await ArrangeAsync();

        await AssertRefusedAndRecordedAsync(await ReadAsync(w), w);
    }

    [Fact]
    public async Task Ended_treatment_revoked_and_expired_consents_do_not_grant_access()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var now = _api.Time.Now;
        await SchemaData.InsertTreatmentAsync(
            owner,
            w.Patient,
            w.Clinician,
            now.AddDays(-10),
            endedAt: now.AddDays(-5)
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            now.AddDays(-9),
            revokedAt: now.AddDays(-4)
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            now.AddDays(-9),
            expiresAt: now
        );

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
    public async Task Someone_elses_clinician_id_is_forbidden_and_records_nothing()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertTreatmentAsync(
            owner,
            w.Patient,
            w.Clinician,
            _api.Time.Now.AddDays(-10)
        );
        var impostor = _api.ClientWith(TestTokens.Create(Guid.NewGuid().ToString(), ["clinician"]));

        var response = await impostor.GetAsync(
            $"/clinicians/{w.Clinician}/patients/{w.Patient}/lab-results"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(await SchemaData.AuditReadsAsync(owner, w.Patient));
    }

    [Fact]
    public async Task A_patient_that_does_not_exist_is_forbidden_like_any_other_refusal()
    {
        var w = await ArrangeAsync();

        var response = await _api.Client.GetAsync(
            $"/clinicians/{w.Clinician}/patients/{Guid.NewGuid()}/lab-results"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task No_results_are_returned_when_the_read_cannot_be_audited()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertTreatmentAsync(
            owner,
            w.Patient,
            w.Clinician,
            _api.Time.Now.AddDays(-10)
        );

        // Tests in this collection run one at a time, so briefly removing the privilege is safe.
        await owner.ExecuteAsync($"REVOKE INSERT ON audit_log FROM {PostgresFixture.AppRole}");
        HttpResponseMessage response;
        try
        {
            response = await ReadAsync(w);
        }
        finally
        {
            await owner.ExecuteAsync($"GRANT INSERT ON audit_log TO {PostgresFixture.AppRole}");
        }

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("LDL", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_read_waits_for_an_in_flight_revoke_and_is_then_refused()
    {
        var w = await ArrangeAsync();
        await using var setup = await postgres.OpenOwnerConnectionAsync();
        var consent = await SchemaData.InsertConsentAtAsync(
            setup,
            w.Patient,
            w.Org,
            _api.Time.Now.AddDays(-1),
            categories: ["lipids", "hematology", "urinalysis"]
        );

        // Simulates a revoke that has locked and updated the consent but not yet committed.
        await using var revoker = await postgres.OpenOwnerConnectionAsync();
        await using var revoke = await revoker.BeginTransactionAsync();
        await revoker.ExecuteAsync(
            "SELECT 1 FROM consent_grants WHERE id = @consent FOR UPDATE",
            new { consent },
            revoke
        );
        await revoker.ExecuteAsync(
            "UPDATE consent_grants SET revoked_at = @at WHERE id = @consent",
            new { at = _api.Time.Now, consent },
            revoke
        );

        var read = ReadAsync(w);
        await Task.Delay(500);
        Assert.False(read.IsCompleted, "The read must wait for the in-flight revoke.");

        await revoke.CommitAsync();

        await AssertRefusedAndRecordedAsync(await read, w);
    }
}
