using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AuthenticationTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);
    private static readonly Guid AnyId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public void Dispose() => _api.Dispose();

    public static TheoryData<string> ProtectedRoutes() =>
        new()
        {
            $"/patients/{AnyId}/lab-results",
            $"/patients/{AnyId}/lab-results/{AnyId}",
            $"/patients/{AnyId}/consents",
            $"/patients/{AnyId}/audit",
            "/organizations",
            $"/clinicians/{AnyId}/patients",
            $"/clinicians/{AnyId}/patients/{AnyId}",
            $"/clinicians/{AnyId}/patients/{AnyId}/lab-results",
            $"/researchers/{AnyId}/patients",
            $"/researchers/{AnyId}/patients/{AnyId}/lab-results",
            $"/treatment-relationships/{AnyId}",
        };

    [Fact]
    public async Task Health_is_open_to_everyone()
    {
        var response = await _api.AnonymousClient.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedRoutes))]
    public async Task Every_route_rejects_a_request_without_a_token(string path)
    {
        var response = await _api.AnonymousClient.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Writes_are_rejected_without_a_token_too()
    {
        var grant = await _api.AnonymousClient.PostAsJsonAsync(
            $"/patients/{AnyId}/consents",
            new { }
        );
        var revoke = await _api.AnonymousClient.DeleteAsync($"/patients/{AnyId}/consents/{AnyId}");
        var start = await _api.AnonymousClient.PostAsJsonAsync("/treatment-relationships", new { });

        Assert.All(
            [grant, revoke, start],
            response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
        );
    }

    public static TheoryData<string, Func<string>> UntrustedTokens() =>
        new()
        {
            { "not a token", () => "not-a-token" },
            {
                "signed with another key",
                () =>
                    TestTokens.Create("x", ["patient"], key: new SymmetricSecurityKey(new byte[32]))
            },
            {
                "another issuer",
                () =>
                    TestTokens.Create(
                        "x",
                        ["patient"],
                        issuer: "http://evil.example/realms/patient-portal"
                    )
            },
            { "another audience", () => TestTokens.Create("x", ["patient"], audience: "account") },
            {
                "expired",
                () => TestTokens.Create("x", ["patient"], lifetime: TimeSpan.FromHours(-2))
            },
        };

    [Theory]
    [MemberData(nameof(UntrustedTokens))]
    public async Task A_token_that_is_not_ours_is_rejected(string _, Func<string> token)
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var client = _api.ClientWith(token());

        var response = await client.GetAsync($"/patients/{patient}/lab-results");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string[]?, string> WrongRoles() =>
        new()
        {
            { ["clinician"], $"/patients/{AnyId}/lab-results" },
            { ["researcher"], $"/patients/{AnyId}/consents" },
            { ["admin"], $"/patients/{AnyId}/audit" },
            { [], $"/patients/{AnyId}/lab-results" },
            { null, $"/patients/{AnyId}/lab-results" },
            { ["patient"], $"/clinicians/{AnyId}/patients" },
            { ["patient"], $"/researchers/{AnyId}/patients" },
            { ["clinician"], $"/researchers/{AnyId}/patients" },
            { ["researcher"], $"/clinicians/{AnyId}/patients/{AnyId}/lab-results" },
            { ["clinician"], "/organizations" },
            { ["patient"], $"/treatment-relationships/{AnyId}" },
            { ["clinician"], $"/treatment-relationships/{AnyId}" },
        };

    [Theory]
    [MemberData(nameof(WrongRoles))]
    public async Task A_valid_token_with_the_wrong_role_is_forbidden(string[]? roles, string path)
    {
        var client = _api.ClientWith(TestTokens.Create(AnyId.ToString(), roles));

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_right_role_is_not_enough_it_must_be_the_person_in_the_url()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var emily = await SchemaData.InsertPatientAsync(owner);
        var james = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var sarah = await SchemaData.InsertClinicianAsync(owner, clinic);
        var michael = await SchemaData.InsertClinicianAsync(owner, clinic);
        var laura = await SchemaData.InsertResearcherAsync(owner, clinic);
        var mei = await SchemaData.InsertResearcherAsync(owner, clinic);

        var emilyClient = _api.ClientWith(TestTokens.Create(emily.ToString(), ["patient"]));
        var sarahClient = _api.ClientWith(TestTokens.Create(sarah.ToString(), ["clinician"]));
        var lauraClient = _api.ClientWith(TestTokens.Create(laura.ToString(), ["researcher"]));

        Assert.Equal(
            HttpStatusCode.OK,
            (await emilyClient.GetAsync($"/patients/{emily}/consents")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await emilyClient.GetAsync($"/patients/{james}/consents")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await emilyClient.GetAsync($"/patients/{james}/audit")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await sarahClient.GetAsync($"/clinicians/{sarah}/patients")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await sarahClient.GetAsync($"/clinicians/{michael}/patients")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await lauraClient.GetAsync($"/researchers/{laura}/patients")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await lauraClient.GetAsync($"/researchers/{mei}/patients")).StatusCode
        );
    }

    [Fact]
    public async Task Writes_for_someone_else_change_nothing()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var emily = await SchemaData.InsertPatientAsync(owner);
        var james = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var consent = await SchemaData.InsertConsentAtAsync(
            owner,
            james,
            clinic,
            _api.Time.Now.AddDays(-1)
        );
        var emilyClient = _api.ClientWith(TestTokens.Create(emily.ToString(), ["patient"]));

        var grant = await emilyClient.PostAsJsonAsync(
            $"/patients/{james}/consents",
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids" },
                purpose = "Care",
            }
        );
        var revoke = await emilyClient.DeleteAsync($"/patients/{james}/consents/{consent}");

        Assert.Equal(HttpStatusCode.Forbidden, grant.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, revoke.StatusCode);
        Assert.Equal(
            1,
            await owner.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM consent_grants WHERE patient_id = @james",
                new { james }
            )
        );
        Assert.Null(
            await owner.ExecuteScalarAsync<DateTimeOffset?>(
                "SELECT revoked_at FROM consent_grants WHERE id = @consent",
                new { consent }
            )
        );
    }

    [Fact]
    public async Task The_token_subject_is_matched_to_the_stored_subject_not_to_the_person_id()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var subject = $"keycloak-{Guid.NewGuid():N}";
        await owner.ExecuteAsync(
            "UPDATE patients SET external_subject_id = @subject WHERE id = @patient",
            new { subject, patient }
        );

        var byId = _api.ClientWith(TestTokens.Create(patient.ToString(), ["patient"]));
        var bySubject = _api.ClientWith(TestTokens.Create(subject, ["patient"]));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await byId.GetAsync($"/patients/{patient}/consents")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (await bySubject.GetAsync($"/patients/{patient}/consents")).StatusCode
        );
    }

    [Fact]
    public async Task A_person_without_a_login_cannot_be_acted_as()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        await owner.ExecuteAsync(
            "UPDATE patients SET external_subject_id = NULL WHERE id = @patient",
            new { patient }
        );
        var client = _api.ClientWith(TestTokens.Create(patient.ToString(), ["patient"]));

        var response = await client.GetAsync($"/patients/{patient}/consents");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_can_manage_treatment_relationships_and_nobody_else_can()
    {
        var admin = _api.ClientWith(TestTokens.Create("admin-subject", ["admin"]));

        var response = await admin.GetAsync($"/treatment-relationships/{AnyId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_patient_can_read_their_own_lab_results_with_a_real_token()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        await SchemaData.InsertLabResultAsync(owner, patient, "lipids", "LDL", _api.Time.Now);
        var client = _api.ClientWith(TestTokens.Create(patient.ToString(), ["patient"]));

        var response = await client.GetAsync($"/patients/{patient}/lab-results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListLabResultsResponse>();
        Assert.Single(body!.Items);
    }
}
