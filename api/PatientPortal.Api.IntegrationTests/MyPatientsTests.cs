using System.Net;
using System.Net.Http.Json;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Clinicians;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class MyPatientsTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private async Task<PagedResponse<PatientSummaryResponse>> GetAsync(
        Guid clinician,
        string query = ""
    )
    {
        var response = await _api.Client.GetAsync($"/clinicians/{clinician}/patients{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<PatientSummaryResponse>>())!;
    }

    [Fact]
    public async Task Lists_patients_reached_by_treatment_or_consent_with_the_basis_for_each()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var otherOrg = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        var colleague = await SchemaData.InsertClinicianAsync(owner, org);
        var now = _api.Time.Now;
        var ago = now.AddDays(-30);

        var treated = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertTreatmentAsync(owner, treated, clinician, ago);
        var consented = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(owner, consented, org, ago);
        var both = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertTreatmentAsync(owner, both, clinician, ago);
        await SchemaData.InsertConsentAtAsync(owner, both, org, ago, expiresAt: now.AddDays(1));

        var endedTreatment = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertTreatmentAsync(
            owner,
            endedTreatment,
            clinician,
            ago,
            endedAt: now.AddDays(-1)
        );
        var revoked = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(owner, revoked, org, ago, revokedAt: now.AddDays(-1));
        var expiredExactlyNow = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(owner, expiredExactlyNow, org, ago, expiresAt: now);
        var otherOrgConsent = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertConsentAtAsync(owner, otherOrgConsent, otherOrg, ago);
        var colleaguesPatient = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertTreatmentAsync(owner, colleaguesPatient, colleague, ago);

        var page = await GetAsync(clinician);

        var basis = page.Items.ToDictionary(item => item.Id, item => item.AccessBasis);
        Assert.Equal(3, basis.Count);
        Assert.Equal(["treatment"], basis[treated]);
        Assert.Equal(["consent"], basis[consented]);
        Assert.Equal(["treatment", "consent"], basis[both]);
    }

    [Fact]
    public async Task Patients_are_ordered_by_name_and_paged()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        foreach (var name in new[] { "Cleo", "Ada", "Bea" })
        {
            var patient = await SchemaData.InsertNamedPatientAsync(owner, name);
            await SchemaData.InsertTreatmentAsync(
                owner,
                patient,
                clinician,
                _api.Time.Now.AddDays(-1)
            );
        }

        var first = await GetAsync(clinician, "?pageSize=2");
        var second = await GetAsync(clinician, "?page=2&pageSize=2");

        Assert.Equal(["Ada", "Bea"], first.Items.Select(item => item.FullName));
        Assert.True(first.HasNextPage);
        Assert.Equal(["Cleo"], second.Items.Select(item => item.FullName));
        Assert.False(second.HasNextPage);
    }

    [Fact]
    public async Task A_clinician_with_no_patients_gets_an_empty_page()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var clinician = await SchemaData.InsertClinicianAsync(
            owner,
            await SchemaData.InsertOrganizationAsync(owner)
        );

        var page = await GetAsync(clinician);

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task An_unknown_clinician_is_not_found()
    {
        var response = await _api.Client.GetAsync($"/clinicians/{Guid.NewGuid()}/patients");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    public async Task Invalid_paging_is_rejected(string query)
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var clinician = await SchemaData.InsertClinicianAsync(
            owner,
            await SchemaData.InsertOrganizationAsync(owner)
        );

        var response = await _api.Client.GetAsync($"/clinicians/{clinician}/patients{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
