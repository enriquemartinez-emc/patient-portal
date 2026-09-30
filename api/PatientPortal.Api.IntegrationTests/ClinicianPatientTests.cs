using System.Net;
using System.Net.Http.Json;
using PatientPortal.Api.Features.Clinicians;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ClinicianPatientTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private async Task<(Guid Org, Guid Clinician, Guid Patient)> ArrangeAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        var patient = await SchemaData.InsertNamedPatientAsync(owner, "Jane Roe");
        return (org, clinician, patient);
    }

    private Task<HttpResponseMessage> GetAsync(Guid clinician, Guid patient) =>
        _api.Client.GetAsync($"/clinicians/{clinician}/patients/{patient}");

    [Fact]
    public async Task Returns_the_patient_with_the_basis_for_the_relationship()
    {
        var (org, clinician, patient) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertTreatmentAsync(owner, patient, clinician, _api.Time.Now.AddDays(-9));
        await SchemaData.InsertConsentAtAsync(owner, patient, org, _api.Time.Now.AddDays(-1));

        var response = await GetAsync(clinician, patient);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PatientSummaryResponse>();
        Assert.Equal("Jane Roe", body!.FullName);
        Assert.Equal(["treatment", "consent"], body.AccessBasis);
    }

    [Fact]
    public async Task A_consent_alone_is_a_basis()
    {
        var (org, clinician, patient) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await SchemaData.InsertConsentAtAsync(owner, patient, org, _api.Time.Now.AddDays(-1));

        var body = await (
            await GetAsync(clinician, patient)
        ).Content.ReadFromJsonAsync<PatientSummaryResponse>();

        Assert.Equal(["consent"], body!.AccessBasis);
    }

    [Fact]
    public async Task A_patient_who_is_not_theirs_is_not_found()
    {
        var (org, clinician, patient) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var now = _api.Time.Now;
        await SchemaData.InsertTreatmentAsync(
            owner,
            patient,
            clinician,
            now.AddDays(-9),
            endedAt: now.AddDays(-1)
        );
        await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            org,
            now.AddDays(-9),
            revokedAt: now.AddDays(-2)
        );
        await SchemaData.InsertConsentAtAsync(owner, patient, org, now.AddDays(-9), expiresAt: now);

        var response = await GetAsync(clinician, patient);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_clinician_or_patient_is_not_found()
    {
        var (_, clinician, patient) = await ArrangeAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(Guid.NewGuid(), patient)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await GetAsync(clinician, Guid.NewGuid())).StatusCode
        );
    }
}
