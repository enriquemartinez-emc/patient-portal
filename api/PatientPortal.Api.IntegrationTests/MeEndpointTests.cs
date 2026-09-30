using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Features.Me;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class MeEndpointTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private async Task<MeResponse> GetMeAsync(string token)
    {
        var response = await _api.ClientWith(token).GetAsync("/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MeResponse>())!;
    }

    [Fact]
    public async Task A_patient_clinician_and_researcher_each_resolve_to_their_own_record()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var patient = await SchemaData.InsertNamedPatientAsync(owner, "Emily Carter");
        var clinician = await SchemaData.InsertNamedClinicianAsync(
            owner,
            org,
            "Dr. Sarah Thompson"
        );
        var researcher = await SchemaData.InsertResearcherAsync(owner, org);

        var asPatient = await GetMeAsync(TestTokens.Create(patient.ToString(), ["patient"]));
        var asClinician = await GetMeAsync(TestTokens.Create(clinician.ToString(), ["clinician"]));
        var asResearcher = await GetMeAsync(
            TestTokens.Create(researcher.ToString(), ["researcher"])
        );

        Assert.Equal(new MeResponse("patient", patient, "Emily Carter"), asPatient);
        Assert.Equal(new MeResponse("clinician", clinician, "Dr. Sarah Thompson"), asClinician);
        Assert.Equal("researcher", asResearcher.Kind);
        Assert.Equal(researcher, asResearcher.Id);
    }

    [Fact]
    public async Task The_login_subject_is_what_links_a_person_not_their_id()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var subject = $"keycloak-{Guid.NewGuid():N}";
        await owner.ExecuteAsync(
            "UPDATE patients SET external_subject_id = @subject WHERE id = @patient",
            new { subject, patient }
        );

        var me = await GetMeAsync(TestTokens.Create(subject, ["patient"]));

        Assert.Equal(patient, me.Id);
    }

    [Fact]
    public async Task A_login_with_no_matching_record_has_no_portal_profile()
    {
        var admin = await _api.ClientWith(TestTokens.Create("admin-subject", ["admin"]))
            .GetAsync("/me");
        var stranger = await _api.ClientWith(
                TestTokens.Create(Guid.NewGuid().ToString(), ["patient"])
            )
            .GetAsync("/me");

        Assert.Equal(HttpStatusCode.NotFound, admin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, stranger.StatusCode);
    }

    [Fact]
    public async Task Only_the_tables_for_the_roles_held_are_consulted()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var clinician = await SchemaData.InsertClinicianAsync(
            owner,
            await SchemaData.InsertOrganizationAsync(owner)
        );

        // The subject belongs to a clinician, but the token claims to be a patient.
        var response = await _api.ClientWith(TestTokens.Create(clinician.ToString(), ["patient"]))
            .GetAsync("/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_login_matching_more_than_one_kind_of_record_is_a_conflict()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var subject = $"both-{Guid.NewGuid():N}";
        await owner.ExecuteAsync(
            """
            INSERT INTO patients (full_name, date_of_birth, external_subject_id) VALUES ('Both', '1990-01-01', @subject);
            INSERT INTO clinicians (organization_id, full_name, external_subject_id) VALUES (@org, 'Both', @subject);
            """,
            new { subject, org }
        );

        var response = await _api.ClientWith(TestTokens.Create(subject, ["patient", "clinician"]))
            .GetAsync("/me");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Without_a_token_the_answer_is_unauthorized()
    {
        var response = await _api.AnonymousClient.GetAsync("/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
