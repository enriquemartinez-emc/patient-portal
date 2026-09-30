using System.Net;
using System.Net.Http.Json;
using Dapper;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.IntegrationTests;

// These tests race real requests against each other. Which read wins the race varies from run to
// run, so they assert what must hold whatever the order: nothing is served after a revoke has
// finished, and every served read and every refusal is recorded exactly once.
[Collection(PostgresCollection.Name)]
public sealed class ConcurrencyTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private sealed record World(Guid Org, Guid Clinician, Guid Patient);

    private async Task<World> ArrangeAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        var patient = await SchemaData.InsertPatientAsync(owner);
        var at = _api.Time.Now.AddDays(-2);
        foreach (
            var (category, test) in new[]
            {
                ("lipids", "LDL"),
                ("hematology", "Hemoglobin"),
                ("urinalysis", "Protein"),
            }
        )
        {
            await SchemaData.InsertLabResultAsync(owner, patient, category, test, at);
        }
        return new World(org, clinician, patient);
    }

    private Task<HttpResponseMessage> ReadAsync(World w) =>
        _api.Client.GetAsync($"/clinicians/{w.Clinician}/patients/{w.Patient}/lab-results");

    private async Task<(
        HttpStatusCode[] Before,
        HttpStatusCode[] After,
        List<int> ServedCounts
    )> ReadsAroundAsync(
        World w,
        Func<Task> change,
        HttpStatusCode settledStatus,
        int before = 24,
        int after = 12
    )
    {
        var served = new List<int>();
        var settled = 0;
        async Task<HttpStatusCode> ReadOnceAsync()
        {
            var response = await ReadAsync(w);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadFromJsonAsync<ListLabResultsResponse>();
                lock (served)
                    served.Add(body!.Items.Count);
            }
            if (response.StatusCode == settledStatus)
            {
                Interlocked.Increment(ref settled);
            }
            return response.StatusCode;
        }

        var early = Enumerable.Range(0, before).Select(_ => Task.Run(ReadOnceAsync)).ToArray();
        var changing = Task.Run(async () =>
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (Volatile.Read(ref settled) < 3 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(1);
            }
            await change();
        });
        await Task.WhenAll(early.Cast<Task>().Append(changing));

        var late = await Task.WhenAll(
            Enumerable.Range(0, after).Select(_ => Task.Run(ReadOnceAsync))
        );
        return ([.. early.Select(t => t.Result)], late, served);
    }

    private async Task<Dictionary<string, int>> AuditCountsAsync(World w)
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var rows = await owner.QueryAsync<(string Action, long Count)>(
            "SELECT action, count(*) FROM audit_log WHERE patient_id = @patient AND actor_id = @clinician GROUP BY action",
            new { patient = w.Patient, clinician = w.Clinician }
        );
        return rows.ToDictionary(row => row.Action, row => (int)row.Count);
    }

    [Fact]
    public async Task Parallel_grants_are_all_kept_and_each_one_is_recorded()
    {
        var w = await ArrangeAsync();
        const int grants = 12;

        var responses = await Task.WhenAll(
            Enumerable
                .Range(0, grants)
                .Select(i =>
                    Task.Run(() =>
                        _api.Client.PostAsJsonAsync(
                            $"/patients/{w.Patient}/consents",
                            new
                            {
                                granteeOrganizationId = w.Org,
                                categories = new[] { "lipids" },
                                purpose = $"Grant {i}",
                            }
                        )
                    )
                )
        );

        Assert.All(
            responses,
            response => Assert.Equal(HttpStatusCode.Created, response.StatusCode)
        );
        var returned = (
            await Task.WhenAll(
                responses.Select(r => r.Content.ReadFromJsonAsync<ConsentResponse>())
            )
        )
            .Select(consent => consent!.Id)
            .ToList();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var stored = (
            await owner.QueryAsync<Guid>(
                "SELECT id FROM consent_grants WHERE patient_id = @patient",
                new { patient = w.Patient }
            )
        ).ToList();
        var audited = (
            await owner.QueryAsync<Guid>(
                "SELECT consent_grant_id FROM audit_log WHERE patient_id = @patient AND action = 'consent_granted'",
                new { patient = w.Patient }
            )
        ).ToList();
        Assert.Equal(grants, returned.Distinct().Count());
        Assert.Equal(returned.Order(), stored.Order());
        Assert.Equal(returned.Order(), audited.Order());
    }

    [Fact]
    public async Task Reads_racing_a_revoke_are_never_served_after_it_and_every_outcome_is_recorded()
    {
        var w = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var consent = await SchemaData.InsertConsentAtAsync(
            owner,
            w.Patient,
            w.Org,
            _api.Time.Now.AddDays(-1),
            categories: ["lipids", "hematology", "urinalysis"]
        );
        HttpStatusCode revoked = default;

        var (before, after, served) = await ReadsAroundAsync(
            w,
            async () =>
                revoked = (
                    await _api.Client.DeleteAsync($"/patients/{w.Patient}/consents/{consent}")
                ).StatusCode,
            HttpStatusCode.OK
        );

        Assert.Equal(HttpStatusCode.NoContent, revoked);
        Assert.All(after, status => Assert.Equal(HttpStatusCode.Forbidden, status));
        Assert.All(
            before,
            status => Assert.Contains(status, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden })
        );
        Assert.All(served, count => Assert.Equal(3, count));
        var counts = await AuditCountsAsync(w);
        Assert.Equal(served.Count, counts.GetValueOrDefault("lab_results_read"));
        Assert.Equal(
            before.Length + after.Length - served.Count,
            counts.GetValueOrDefault("access_denied")
        );
    }

    [Fact]
    public async Task Reads_racing_a_grant_are_refused_or_served_whole_and_all_are_served_after_it()
    {
        var w = await ArrangeAsync();
        HttpStatusCode granted = default;

        var (before, after, served) = await ReadsAroundAsync(
            w,
            async () =>
                granted = (
                    await _api.Client.PostAsJsonAsync(
                        $"/patients/{w.Patient}/consents",
                        new
                        {
                            granteeOrganizationId = w.Org,
                            categories = new[] { "lipids", "hematology", "urinalysis" },
                            purpose = "Care",
                        }
                    )
                ).StatusCode,
            HttpStatusCode.Forbidden
        );

        Assert.Equal(HttpStatusCode.Created, granted);
        Assert.All(after, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.All(
            before,
            status => Assert.Contains(status, new[] { HttpStatusCode.OK, HttpStatusCode.Forbidden })
        );
        Assert.All(served, count => Assert.Equal(3, count));
        var counts = await AuditCountsAsync(w);
        Assert.Equal(served.Count, counts.GetValueOrDefault("lab_results_read"));
        Assert.Equal(
            before.Length + after.Length - served.Count,
            counts.GetValueOrDefault("access_denied")
        );
    }
}
