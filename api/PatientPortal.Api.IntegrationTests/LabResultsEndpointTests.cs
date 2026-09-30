using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class LabResultsEndpointTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task Listing_returns_only_the_patients_results_newest_first()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var other = await SchemaData.InsertPatientAsync(owner);
        var now = DateTimeOffset.UtcNow;
        var older = await SchemaData.InsertLabResultAsync(
            owner,
            patient,
            "lipids",
            "LDL",
            now.AddDays(-10)
        );
        var newer = await SchemaData.InsertLabResultAsync(
            owner,
            patient,
            "hematology",
            "Hemoglobin",
            now.AddDays(-1)
        );
        await SchemaData.InsertLabResultAsync(owner, other, "lipids", "LDL", now);

        var response = await _api.Client.GetAsync($"/patients/{patient}/lab-results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListLabResultsResponse>();
        Assert.Equal([newer, older], body!.Items.Select(item => item.Id));
        var first = body.Items[0];
        Assert.Equal("hematology", first.Category);
        Assert.Equal("Hemoglobin", first.TestName);
        Assert.Equal(4.2m, first.Value);
        Assert.Equal("mmol/L", first.Unit);
    }

    [Fact]
    public async Task Listing_for_a_patient_without_results_is_empty()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await _api.Client.GetAsync($"/patients/{patient}/lab-results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListLabResultsResponse>();
        Assert.Empty(body!.Items);
    }

    [Fact]
    public async Task Listing_for_an_unknown_patient_is_not_found()
    {
        var response = await _api.Client.GetAsync($"/patients/{Guid.NewGuid()}/lab-results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Getting_a_result_returns_it()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var result = await SchemaData.InsertLabResultAsync(
            owner,
            patient,
            "lipids",
            "LDL",
            DateTimeOffset.UtcNow
        );

        var response = await _api.Client.GetAsync($"/patients/{patient}/lab-results/{result}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LabResultResponse>();
        Assert.Equal(result, body!.Id);
        Assert.Equal("LDL", body.TestName);
    }

    [Fact]
    public async Task Getting_another_patients_result_is_not_found()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var other = await SchemaData.InsertPatientAsync(owner);
        var othersResult = await SchemaData.InsertLabResultAsync(
            owner,
            other,
            "lipids",
            "LDL",
            DateTimeOffset.UtcNow
        );

        var response = await _api.Client.GetAsync(
            $"/patients/{patient}/lab-results/{othersResult}"
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Getting_an_unknown_result_is_not_found()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await _api.Client.GetAsync(
            $"/patients/{patient}/lab-results/{Guid.NewGuid()}"
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_patient_reading_their_own_results_is_not_written_to_the_audit_trail()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var result = await SchemaData.InsertLabResultAsync(
            owner,
            patient,
            "lipids",
            "LDL",
            DateTimeOffset.UtcNow
        );

        await _api.Client.GetAsync($"/patients/{patient}/lab-results");
        await _api.Client.GetAsync($"/patients/{patient}/lab-results/{result}");

        var entries = await owner.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM audit_log WHERE patient_id = @patient",
            new { patient }
        );
        Assert.Equal(0, entries);
    }
}
