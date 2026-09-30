using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Features.Consents;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class GrantConsentTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private Task<HttpResponseMessage> GrantAsync(Guid patient, object body) =>
        _api.Client.PostAsJsonAsync($"/patients/{patient}/consents", body);

    [Fact]
    public async Task Granting_creates_an_active_consent_and_locates_it()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertNamedOrganizationAsync(owner, "Harbor Clinic");

        var response = await GrantAsync(
            patient,
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids", "hematology" },
                purpose = "Second opinion",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConsentResponse>();
        Assert.Equal("active", body!.Status);
        Assert.Equal(clinic, body.GranteeOrganizationId);
        Assert.Equal("Harbor Clinic", body.GranteeName);
        Assert.Equal(["lipids", "hematology"], body.Categories);
        Assert.Equal("Second opinion", body.Purpose);
        Assert.Equal(_api.Time.Now, body.GrantedAt);
        Assert.Null(body.ExpiresAt);
        Assert.Null(body.RevokedAt);
        Assert.EndsWith(
            $"/patients/{patient}/consents/{body.Id}",
            response.Headers.Location!.ToString()
        );

        var located = await _api.Client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, located.StatusCode);
    }

    [Fact]
    public async Task Granting_is_recorded_in_the_audit_trail_as_an_action_of_the_patient()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);

        var response = await GrantAsync(
            patient,
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids" },
                purpose = "Care",
            }
        );
        var consent = (await response.Content.ReadFromJsonAsync<ConsentResponse>())!.Id;

        var entries = (
            await owner.QueryAsync<AuditRow>(
                """
                SELECT actor_kind AS ActorKind, actor_id AS ActorId, action AS Action,
                       consent_grant_id AS ConsentGrantId, occurred_at AS OccurredAt
                FROM audit_log WHERE patient_id = @patient
                """,
                new { patient }
            )
        ).ToList();
        var entry = Assert.Single(entries);
        Assert.Equal("patient", entry.ActorKind);
        Assert.Equal(patient, entry.ActorId);
        Assert.Equal("consent_granted", entry.Action);
        Assert.Equal(consent, entry.ConsentGrantId);
        Assert.Equal(_api.Time.Now, entry.OccurredAt);
    }

    [Fact]
    public async Task Duplicate_categories_are_stored_once()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);

        var response = await GrantAsync(
            patient,
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids", "lipids", "hematology" },
                purpose = "Care",
            }
        );

        var body = await response.Content.ReadFromJsonAsync<ConsentResponse>();
        Assert.Equal(["lipids", "hematology"], body!.Categories);
    }

    [Fact]
    public async Task A_future_expiry_is_kept_and_normalized_to_utc()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var expiresAt = _api.Time.Now.AddDays(30).ToOffset(TimeSpan.FromHours(5));

        var response = await GrantAsync(
            patient,
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids" },
                purpose = "Care",
                expiresAt,
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConsentResponse>();
        Assert.Equal(expiresAt, body!.ExpiresAt);
        var stored = await owner.ExecuteScalarAsync<DateTimeOffset>(
            "SELECT expires_at FROM consent_grants WHERE id = @id",
            new { id = body.Id }
        );
        Assert.Equal(expiresAt, stored);
    }

    public static TheoryData<string, object> InvalidRequests() =>
        new()
        {
            {
                "Categories",
                new
                {
                    granteeOrganizationId = Guid.NewGuid(),
                    categories = Array.Empty<string>(),
                    purpose = "Care",
                }
            },
            { "Categories", new { granteeOrganizationId = Guid.NewGuid(), purpose = "Care" } },
            {
                "Categories[0]",
                new
                {
                    granteeOrganizationId = Guid.NewGuid(),
                    categories = new[] { "not_a_category" },
                    purpose = "Care",
                }
            },
            {
                "Purpose",
                new
                {
                    granteeOrganizationId = Guid.NewGuid(),
                    categories = new[] { "lipids" },
                    purpose = "  ",
                }
            },
            {
                "Purpose",
                new
                {
                    granteeOrganizationId = Guid.NewGuid(),
                    categories = new[] { "lipids" },
                    purpose = new string('x', 501),
                }
            },
            {
                "GranteeOrganizationId",
                new
                {
                    granteeOrganizationId = Guid.Empty,
                    categories = new[] { "lipids" },
                    purpose = "Care",
                }
            },
            {
                "ExpiresAt",
                new
                {
                    granteeOrganizationId = Guid.NewGuid(),
                    categories = new[] { "lipids" },
                    purpose = "Care",
                    expiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                }
            },
        };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_requests_are_rejected_with_a_validation_problem(
        string invalidField,
        object body
    )
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await GrantAsync(patient, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        Assert.Contains(invalidField, problem!.Errors.Keys);
        var stored = await owner.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM consent_grants WHERE patient_id = @patient",
            new { patient }
        );
        Assert.Equal(0, stored);
    }

    [Fact]
    public async Task An_unknown_grantee_is_rejected_and_nothing_is_written()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await GrantAsync(
            patient,
            new
            {
                granteeOrganizationId = Guid.NewGuid(),
                categories = new[] { "lipids" },
                purpose = "Care",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await CountForAsync(owner, "consent_grants", patient));
        Assert.Equal(0, await CountForAsync(owner, "audit_log", patient));
    }

    [Fact]
    public async Task Granting_for_someone_elses_patient_id_is_forbidden()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var clinic = await SchemaData.InsertOrganizationAsync(owner);

        var response = await GrantAsync(
            Guid.NewGuid(),
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids" },
                purpose = "Care",
            }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_consent_is_not_kept_when_its_audit_entry_cannot_be_written()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);

        // Tests in this collection run one at a time, so briefly removing the privilege is safe.
        await owner.ExecuteAsync($"REVOKE INSERT ON audit_log FROM {PostgresFixture.AppRole}");
        HttpResponseMessage response;
        try
        {
            response = await GrantAsync(
                patient,
                new
                {
                    granteeOrganizationId = clinic,
                    categories = new[] { "lipids" },
                    purpose = "Care",
                }
            );
        }
        finally
        {
            await owner.ExecuteAsync($"GRANT INSERT ON audit_log TO {PostgresFixture.AppRole}");
        }

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(0, await CountForAsync(owner, "consent_grants", patient));
    }

    private static Task<int> CountForAsync(
        Npgsql.NpgsqlConnection owner,
        string table,
        Guid patient
    ) =>
        owner.ExecuteScalarAsync<int>(
            $"SELECT count(*) FROM {table} WHERE patient_id = @patient",
            new { patient }
        );

    private sealed record ValidationProblem(Dictionary<string, string[]> Errors);

    private sealed record AuditRow(
        string ActorKind,
        Guid ActorId,
        string Action,
        Guid? ConsentGrantId,
        DateTimeOffset OccurredAt
    );
}
