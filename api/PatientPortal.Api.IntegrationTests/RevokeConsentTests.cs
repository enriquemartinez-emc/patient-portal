using System.Net;
using Dapper;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class RevokeConsentTests(PostgresFixture postgres) : IDisposable
{
    private readonly ApiHarness _api = new(postgres);

    public void Dispose() => _api.Dispose();

    private Task<HttpResponseMessage> RevokeAsync(Guid patient, Guid consent) =>
        _api.Client.DeleteAsync($"/patients/{patient}/consents/{consent}");

    private static Task<List<AuditRow>> AuditEntriesAsync(
        Npgsql.NpgsqlConnection owner,
        Guid patient
    ) =>
        owner
            .QueryAsync<AuditRow>(
                """
                SELECT actor_kind AS ActorKind, actor_id AS ActorId, action AS Action,
                       consent_grant_id AS ConsentGrantId, occurred_at AS OccurredAt
                FROM audit_log WHERE patient_id = @patient ORDER BY occurred_at, id
                """,
                new { patient }
            )
            .ContinueWith(t => t.Result.ToList());

    private async Task<(Guid Patient, Guid Consent)> ArrangeActiveConsentAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var consent = await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            clinic,
            _api.Time.Now.AddDays(-1)
        );
        return (patient, consent);
    }

    [Fact]
    public async Task Revoking_an_active_consent_stamps_it_and_records_the_action()
    {
        var (patient, consent) = await ArrangeActiveConsentAsync();

        var response = await RevokeAsync(patient, consent);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var revokedAt = await owner.ExecuteScalarAsync<DateTimeOffset?>(
            "SELECT revoked_at FROM consent_grants WHERE id = @consent",
            new { consent }
        );
        Assert.Equal(_api.Time.Now, revokedAt);
        var entry = Assert.Single(await AuditEntriesAsync(owner, patient));
        Assert.Equal("patient", entry.ActorKind);
        Assert.Equal(patient, entry.ActorId);
        Assert.Equal("consent_revoked", entry.Action);
        Assert.Equal(consent, entry.ConsentGrantId);
    }

    [Fact]
    public async Task Revoking_twice_succeeds_and_records_only_the_first()
    {
        var (patient, consent) = await ArrangeActiveConsentAsync();
        await RevokeAsync(patient, consent);
        _api.Time.Now = _api.Time.Now.AddHours(1);

        var second = await RevokeAsync(patient, consent);

        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        Assert.Single(await AuditEntriesAsync(owner, patient));
        var revokedAt = await owner.ExecuteScalarAsync<DateTimeOffset?>(
            "SELECT revoked_at FROM consent_grants WHERE id = @consent",
            new { consent }
        );
        Assert.Equal(_api.Time.Now.AddHours(-1), revokedAt);
    }

    [Fact]
    public async Task Concurrent_revokes_record_exactly_one_audit_entry()
    {
        var (patient, consent) = await ArrangeActiveConsentAsync();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => RevokeAsync(patient, consent))
        );

        Assert.All(
            responses,
            response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode)
        );
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        Assert.Single(await AuditEntriesAsync(owner, patient));
    }

    [Fact]
    public async Task An_expired_consent_cannot_be_revoked_and_records_nothing()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var clinic = await SchemaData.InsertOrganizationAsync(owner);
        var consent = await SchemaData.InsertConsentAtAsync(
            owner,
            patient,
            clinic,
            grantedAt: _api.Time.Now.AddDays(-10),
            expiresAt: _api.Time.Now.AddDays(-1)
        );

        var response = await RevokeAsync(patient, consent);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await AuditEntriesAsync(owner, patient));
        Assert.Null(
            await owner.ExecuteScalarAsync<DateTimeOffset?>(
                "SELECT revoked_at FROM consent_grants WHERE id = @consent",
                new { consent }
            )
        );
    }

    [Fact]
    public async Task Another_patients_consent_cannot_be_revoked()
    {
        var (_, othersConsent) = await ArrangeActiveConsentAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        var response = await RevokeAsync(patient, othersConsent);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(
            await owner.ExecuteScalarAsync<DateTimeOffset?>(
                "SELECT revoked_at FROM consent_grants WHERE id = @othersConsent",
                new { othersConsent }
            )
        );
    }

    [Fact]
    public async Task Revoking_an_unknown_consent_is_not_found()
    {
        var (patient, _) = await ArrangeActiveConsentAsync();

        var response = await RevokeAsync(patient, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record AuditRow(
        string ActorKind,
        Guid ActorId,
        string Action,
        Guid? ConsentGrantId,
        DateTimeOffset OccurredAt
    );
}
