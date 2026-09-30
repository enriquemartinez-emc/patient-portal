using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Features.Treatment;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class TreatmentEndpointTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private Task<HttpResponseMessage> StartAsync(Guid patient, Guid clinician) =>
        _api.Client.PostAsJsonAsync(
            "/treatment-relationships",
            new { patientId = patient, clinicianId = clinician }
        );

    private async Task<(Guid Patient, Guid Clinician)> ArrangeAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(
            owner,
            await SchemaData.InsertOrganizationAsync(owner)
        );
        return (patient, clinician);
    }

    [Fact]
    public async Task Starting_creates_an_active_relationship_and_locates_it()
    {
        var (patient, clinician) = await ArrangeAsync();

        var response = await StartAsync(patient, clinician);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TreatmentRelationshipResponse>();
        Assert.Equal("active", body!.Status);
        Assert.Equal(patient, body.PatientId);
        Assert.Equal(clinician, body.ClinicianId);
        Assert.Equal(_api.Time.Now, body.StartedAt);
        Assert.Null(body.EndedAt);
        Assert.EndsWith(
            $"/treatment-relationships/{body.Id}",
            response.Headers.Location!.ToString()
        );
        var located = await _api.Client.GetFromJsonAsync<TreatmentRelationshipResponse>(
            response.Headers.Location
        );
        Assert.Equal(body.Id, located!.Id);
    }

    [Fact]
    public async Task A_second_active_relationship_for_the_same_pair_is_a_conflict()
    {
        var (patient, clinician) = await ArrangeAsync();
        await StartAsync(patient, clinician);

        var response = await StartAsync(patient, clinician);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var count = await owner.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM treatment_relationships WHERE patient_id = @patient",
            new { patient }
        );
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task A_relationship_can_start_again_after_it_ended()
    {
        var (patient, clinician) = await ArrangeAsync();
        var first = await (
            await StartAsync(patient, clinician)
        ).Content.ReadFromJsonAsync<TreatmentRelationshipResponse>();
        await _api.Client.DeleteAsync($"/treatment-relationships/{first!.Id}");

        var response = await StartAsync(patient, clinician);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_starts_for_the_same_pair_create_exactly_one()
    {
        var (patient, clinician) = await ArrangeAsync();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => StartAsync(patient, clinician))
        );

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Starting_with_an_unknown_patient_or_clinician_is_unprocessable()
    {
        var (patient, clinician) = await ArrangeAsync();

        var unknownPatient = await StartAsync(Guid.NewGuid(), clinician);
        var unknownClinician = await StartAsync(patient, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownPatient.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownClinician.StatusCode);
    }

    [Fact]
    public async Task Starting_without_ids_is_a_validation_problem()
    {
        var response = await StartAsync(Guid.Empty, Guid.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ending_stamps_the_relationship_and_it_reads_back_as_ended()
    {
        var (patient, clinician) = await ArrangeAsync();
        var started = await (
            await StartAsync(patient, clinician)
        ).Content.ReadFromJsonAsync<TreatmentRelationshipResponse>();
        _api.Time.Now = _api.Time.Now.AddDays(3);

        var response = await _api.Client.DeleteAsync($"/treatment-relationships/{started!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ended = await _api.Client.GetFromJsonAsync<TreatmentRelationshipResponse>(
            $"/treatment-relationships/{started.Id}"
        );
        Assert.Equal("ended", ended!.Status);
        Assert.Equal(_api.Time.Now, ended.EndedAt);
    }

    [Fact]
    public async Task Ending_twice_succeeds_and_keeps_the_first_end_time()
    {
        var (patient, clinician) = await ArrangeAsync();
        var started = await (
            await StartAsync(patient, clinician)
        ).Content.ReadFromJsonAsync<TreatmentRelationshipResponse>();
        await _api.Client.DeleteAsync($"/treatment-relationships/{started!.Id}");
        var firstEnd = _api.Time.Now;
        _api.Time.Now = firstEnd.AddDays(1);

        var response = await _api.Client.DeleteAsync($"/treatment-relationships/{started.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ended = await _api.Client.GetFromJsonAsync<TreatmentRelationshipResponse>(
            $"/treatment-relationships/{started.Id}"
        );
        Assert.Equal(firstEnd, ended!.EndedAt);
    }

    [Fact]
    public async Task An_unknown_relationship_is_not_found()
    {
        var get = await _api.Client.GetAsync($"/treatment-relationships/{Guid.NewGuid()}");
        var end = await _api.Client.DeleteAsync($"/treatment-relationships/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, end.StatusCode);
    }
}
