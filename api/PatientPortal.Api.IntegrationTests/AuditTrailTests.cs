using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Audit;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AuditTrailTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private async Task<PagedResponse<AuditEntryResponse>> GetPageAsync(
        Guid patient,
        string query = ""
    )
    {
        var response = await _api.Client.GetAsync($"/patients/{patient}/audit{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<AuditEntryResponse>>())!;
    }

    [Fact]
    public async Task The_trail_shows_who_accessed_the_record_newest_first_with_names_resolved()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var other = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertNamedOrganizationAsync(owner, "Harbor Clinic");
        var clinician = await SchemaData.InsertNamedClinicianAsync(owner, clinic, "Dr. Ada Vale");
        var now = DateTimeOffset.UtcNow;
        var older = await SchemaData.InsertAuditEntryAtAsync(
            owner,
            patient,
            "clinician",
            clinician,
            now.AddHours(-2)
        );
        var newer = await SchemaData.InsertAuditEntryAtAsync(
            owner,
            patient,
            "patient",
            patient,
            now.AddHours(-1)
        );
        await SchemaData.InsertAuditEntryAtAsync(owner, other, "clinician", clinician, now);

        var page = await GetPageAsync(patient);

        Assert.Equal([newer, older], page.Items.Select(item => item.Id));
        var clinicianEntry = page.Items[1];
        Assert.Equal("clinician", clinicianEntry.ActorKind);
        Assert.Equal("Dr. Ada Vale", clinicianEntry.ActorName);
        Assert.Equal("Harbor Clinic", clinicianEntry.ActorOrganization);
        Assert.Equal("lab_results_read", clinicianEntry.Action);
        var patientEntry = page.Items[0];
        Assert.Equal("patient", patientEntry.ActorKind);
        Assert.Equal("Test Patient", patientEntry.ActorName);
        Assert.Null(patientEntry.ActorOrganization);
    }

    [Fact]
    public async Task A_read_lists_the_lab_results_that_were_returned()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, clinic);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await owner.ExecuteAsync(
            """
            INSERT INTO audit_log (actor_kind, actor_id, patient_id, action, lab_result_ids)
            VALUES ('clinician', @clinician, @patient, 'lab_results_read', @ids)
            """,
            new
            {
                clinician,
                patient,
                ids = new[] { first, second },
            }
        );

        var page = await GetPageAsync(patient);

        var entry = Assert.Single(page.Items);
        Assert.Equal([first, second], entry.LabResultIds);
        Assert.Null(entry.ConsentGrantId);
    }

    [Fact]
    public async Task Consent_actions_taken_through_the_api_appear_in_the_trail()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var grant = await _api.Client.PostAsJsonAsync(
            $"/patients/{patient}/consents",
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids" },
                purpose = "Care",
            }
        );
        var consent = (
            await grant.Content.ReadFromJsonAsync<Features.Consents.ConsentResponse>()
        )!.Id;
        _api.Time.Now = _api.Time.Now.AddMinutes(5);
        await _api.Client.DeleteAsync($"/patients/{patient}/consents/{consent}");

        var page = await GetPageAsync(patient);

        Assert.Equal(
            ["consent_revoked", "consent_granted"],
            page.Items.Select(item => item.Action)
        );
        Assert.All(page.Items, item => Assert.Equal(consent, item.ConsentGrantId));
        Assert.All(page.Items, item => Assert.Equal("patient", item.ActorKind));
    }

    [Fact]
    public async Task Paging_walks_the_whole_trail_without_gaps_or_repeats()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var now = DateTimeOffset.UtcNow;
        var ids = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            ids.Add(
                await SchemaData.InsertAuditEntryAtAsync(
                    owner,
                    patient,
                    "patient",
                    patient,
                    now.AddMinutes(-i)
                )
            );
        }

        var first = await GetPageAsync(patient, "?page=1&pageSize=2");
        var second = await GetPageAsync(patient, "?page=2&pageSize=2");
        var third = await GetPageAsync(patient, "?page=3&pageSize=2");

        Assert.Equal([2, 2, 1], [first.Items.Count, second.Items.Count, third.Items.Count]);
        Assert.Equal(
            [true, true, false],
            [first.HasNextPage, second.HasNextPage, third.HasNextPage]
        );
        Assert.Equal(
            ids,
            first.Items.Concat(second.Items).Concat(third.Items).Select(item => item.Id)
        );
    }

    [Fact]
    public async Task Entries_with_the_same_timestamp_page_in_a_stable_order()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var at = DateTimeOffset.UtcNow;
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add(
                await SchemaData.InsertAuditEntryAtAsync(owner, patient, "patient", patient, at)
            );
        }

        var first = await GetPageAsync(patient, "?page=1&pageSize=2");
        var second = await GetPageAsync(patient, "?page=2&pageSize=2");

        var walked = first.Items.Concat(second.Items).Select(item => item.Id).ToList();
        Assert.Equal(3, walked.Distinct().Count());
        Assert.Equal(ids.OrderByDescending(id => id), walked);
    }

    [Fact]
    public async Task A_trail_with_no_entries_is_an_empty_first_page()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var page = await GetPageAsync(patient);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(PagingRules.DefaultPageSize, page.PageSize);
        Assert.False(page.HasNextPage);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    public async Task Invalid_paging_is_rejected(string query)
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await _api.Client.GetAsync($"/patients/{patient}/audit{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_trail_of_someone_elses_patient_id_is_forbidden()
    {
        var response = await _api.Client.GetAsync($"/patients/{Guid.NewGuid()}/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_action_filter_returns_only_matching_entries_and_pages_them()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, clinic);
        await SchemaData.InsertAuditEntryAtAsync(
            owner,
            patient,
            "clinician",
            clinician,
            _api.Time.Now.AddHours(-3)
        );
        await SchemaData.InsertAuditEntryAtAsync(
            owner,
            patient,
            "clinician",
            clinician,
            _api.Time.Now.AddHours(-2)
        );
        var grant = await _api.Client.PostAsJsonAsync(
            $"/patients/{patient}/consents",
            new
            {
                granteeOrganizationId = clinic,
                categories = new[] { "lipids" },
                purpose = "Care",
            }
        );
        var consent = (
            await grant.Content.ReadFromJsonAsync<Features.Consents.ConsentResponse>()
        )!.Id;
        _api.Time.Now = _api.Time.Now.AddMinutes(5);
        await _api.Client.DeleteAsync($"/patients/{patient}/consents/{consent}");

        var reads = await GetPageAsync(patient, "?action=lab_results_read&pageSize=1");
        var readsPage2 = await GetPageAsync(patient, "?action=lab_results_read&page=2&pageSize=1");
        var revokes = await GetPageAsync(patient, "?action=consent_revoked");
        var everything = await GetPageAsync(patient);

        Assert.Equal(["lab_results_read"], reads.Items.Select(item => item.Action));
        Assert.True(reads.HasNextPage);
        Assert.Equal(["lab_results_read"], readsPage2.Items.Select(item => item.Action));
        Assert.False(readsPage2.HasNextPage);
        Assert.Equal(["consent_revoked"], revokes.Items.Select(item => item.Action));
        Assert.Equal(4, everything.Items.Count);
    }

    [Fact]
    public async Task Refused_access_attempts_can_be_filtered_like_any_other_action()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner, "clinic");
        var clinician = await SchemaData.InsertClinicianAsync(owner, clinic);
        await SchemaData.InsertAuditEntryAtAsync(
            owner,
            patient,
            "clinician",
            clinician,
            _api.Time.Now.AddHours(-2)
        );
        await owner.ExecuteAsync(
            """
            INSERT INTO audit_log (occurred_at, actor_kind, actor_id, patient_id, action)
            VALUES (@occurredAt, 'clinician', @clinician, @patient, 'access_denied')
            """,
            new
            {
                occurredAt = _api.Time.Now.AddHours(-1),
                clinician,
                patient,
            }
        );

        var refused = await GetPageAsync(patient, "?action=access_denied");

        Assert.Equal(["access_denied"], refused.Items.Select(item => item.Action));
    }

    [Fact]
    public async Task An_unknown_action_filter_is_rejected()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await _api.Client.GetAsync(
            $"/patients/{patient}/audit?action=deleted_everything"
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
