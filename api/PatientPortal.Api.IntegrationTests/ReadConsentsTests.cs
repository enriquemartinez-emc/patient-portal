using System.Net;
using System.Net.Http.Json;
using PatientPortal.Api.Features.Consents;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class ReadConsentsTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Listing_shows_each_consent_with_its_state_newest_first()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var other = await SchemaData.InsertPatientAsync(owner);
        var north = await SchemaData.InsertNamedOrganizationAsync(owner, "North Clinic");
        var south = await SchemaData.InsertNamedOrganizationAsync(owner, "South Clinic");
        var east = await SchemaData.InsertNamedOrganizationAsync(owner, "East Clinic");
        var now = _api.Time.Now;
        var revoked = await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            north,
            now.AddDays(-30),
            revokedAt: now.AddDays(-20)
        );
        var expired = await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            south,
            now.AddDays(-20),
            expiresAt: now.AddDays(-5)
        );
        var active = await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            east,
            now.AddDays(-10),
            expiresAt: now.AddDays(5)
        );
        await SchemaData.InsertConsentAtAsync(owner, other, north, now.AddDays(-1));

        var response = await _api.Client.GetAsync($"/patients/{patient}/consents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListConsentsResponse>();
        Assert.Equal([active, expired, revoked], body!.Items.Select(item => item.Id));
        Assert.Equal(["active", "expired", "revoked"], body.Items.Select(item => item.Status));
        Assert.Equal(
            ["East Clinic", "South Clinic", "North Clinic"],
            body.Items.Select(item => item.GranteeName)
        );
        Assert.Equal(now.AddDays(-20), body.Items[2].RevokedAt);
        Assert.Null(body.Items[0].RevokedAt);
    }

    [Fact]
    public async Task A_consent_becomes_expired_as_time_passes_without_any_write()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var expiresAt = _api.Time.Now.AddDays(1);
        var consent = await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            clinic,
            _api.Time.Now.AddDays(-1),
            expiresAt
        );

        var before = await GetAsync(patient, consent);
        _api.Time.Now = expiresAt;
        var atExpiry = await GetAsync(patient, consent);

        Assert.Equal("active", before.Status);
        Assert.Equal("expired", atExpiry.Status);
    }

    [Fact]
    public async Task Listing_for_a_patient_without_consents_is_empty()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await _api.Client.GetAsync($"/patients/{patient}/consents");

        var body = await response.Content.ReadFromJsonAsync<ListConsentsResponse>();
        Assert.Empty(body!.Items);
    }

    [Fact]
    public async Task Listing_for_an_unknown_patient_is_not_found()
    {
        var response = await _api.Client.GetAsync($"/patients/{Guid.NewGuid()}/consents");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Getting_another_patients_consent_is_not_found()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var other = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var othersConsent = await SchemaData.InsertConsentAtAsync(
            owner,
            other,
            clinic,
            _api.Time.Now.AddDays(-1)
        );

        var response = await _api.Client.GetAsync($"/patients/{patient}/consents/{othersConsent}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Getting_an_unknown_consent_is_not_found()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await _api.Client.GetAsync($"/patients/{patient}/consents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ConsentResponse> GetAsync(Guid patient, Guid consent)
    {
        var response = await _api.Client.GetAsync($"/patients/{patient}/consents/{consent}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ConsentResponse>())!;
    }
}
