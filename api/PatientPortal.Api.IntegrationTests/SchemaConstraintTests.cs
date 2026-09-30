using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SchemaConstraintTests(PostgresFixture postgres)
{
    private async Task<(Guid Patient, Guid Clinician, Guid Org)> ArrangeAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        return (patient, clinician, org);
    }

    [Fact]
    public async Task A_clinician_cannot_have_two_active_relationships_with_the_same_patient()
    {
        var (patient, clinician, _) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        const string insert =
            "INSERT INTO treatment_relationships (patient_id, clinician_id, started_at) VALUES (@patient, @clinician, now())";
        await owner.ExecuteAsync(insert, new { patient, clinician });

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(insert, new { patient, clinician })
        );

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }

    [Fact]
    public async Task A_new_relationship_can_start_after_the_previous_one_ended()
    {
        var (patient, clinician, _) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await owner.ExecuteAsync(
            """
            INSERT INTO treatment_relationships (patient_id, clinician_id, started_at, ended_at)
            VALUES (@patient, @clinician, now() - interval '10 days', now() - interval '5 days')
            """,
            new { patient, clinician }
        );

        var inserted = await owner.ExecuteAsync(
            "INSERT INTO treatment_relationships (patient_id, clinician_id, started_at) VALUES (@patient, @clinician, now())",
            new { patient, clinician }
        );

        Assert.Equal(1, inserted);
    }

    [Fact]
    public async Task A_relationship_cannot_end_before_it_started()
    {
        var (patient, clinician, _) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                """
                INSERT INTO treatment_relationships (patient_id, clinician_id, started_at, ended_at)
                VALUES (@patient, @clinician, now(), now() - interval '1 day')
                """,
                new { patient, clinician }
            )
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Theory]
    [InlineData("ARRAY[]::text[]")]
    [InlineData("ARRAY['not_a_category']")]
    public async Task A_consent_must_name_at_least_one_known_category(string categories)
    {
        var (patient, _, org) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                $"""
                INSERT INTO consent_grants (patient_id, grantee_organization_id, categories, purpose, granted_at)
                VALUES (@patient, @org, {categories}, 'Test purpose', now())
                """,
                new { patient, org }
            )
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task A_consent_must_expire_after_it_was_granted()
    {
        var (patient, _, org) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                """
                INSERT INTO consent_grants (patient_id, grantee_organization_id, categories, purpose, granted_at, expires_at)
                VALUES (@patient, @org, ARRAY['lipids'], 'Test purpose', now(), now() - interval '1 day')
                """,
                new { patient, org }
            )
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task A_patient_can_hold_several_grants_to_the_same_organization()
    {
        var (patient, _, org) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        await SchemaData.InsertConsentAsync(owner, patient, org);
        await SchemaData.InsertConsentAsync(owner, patient, org);

        var count = await owner.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM consent_grants WHERE patient_id = @patient",
            new { patient }
        );
        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData("consent_granted")]
    [InlineData("consent_revoked")]
    public async Task A_consent_audit_entry_must_reference_its_consent(string action)
    {
        var (patient, _, _) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                "INSERT INTO audit_log (actor_kind, actor_id, patient_id, action) VALUES ('patient', @patient, @patient, @action)",
                new { patient, action }
            )
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task A_lab_result_read_cannot_reference_a_consent()
    {
        var (patient, _, org) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var consent = await SchemaData.InsertConsentAsync(owner, patient, org);

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                """
                INSERT INTO audit_log (actor_kind, actor_id, patient_id, action, consent_grant_id)
                VALUES ('clinician', gen_random_uuid(), @patient, 'lab_results_read', @consent)
                """,
                new { patient, consent }
            )
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
    }

    [Fact]
    public async Task A_lab_result_read_with_no_results_is_a_valid_audit_entry()
    {
        var (patient, _, _) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var inserted = await owner.ExecuteAsync(
            "INSERT INTO audit_log (actor_kind, actor_id, patient_id, action) VALUES ('clinician', gen_random_uuid(), @patient, 'lab_results_read')",
            new { patient }
        );

        Assert.Equal(1, inserted);
    }

    [Fact]
    public async Task A_refused_access_is_a_valid_audit_entry_without_results_or_consent()
    {
        var (patient, _, _) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var inserted = await owner.ExecuteAsync(
            "INSERT INTO audit_log (actor_kind, actor_id, patient_id, action) VALUES ('clinician', gen_random_uuid(), @patient, 'access_denied')",
            new { patient }
        );

        Assert.Equal(1, inserted);
    }

    [Fact]
    public async Task A_refused_access_cannot_carry_results_or_a_consent()
    {
        var (patient, _, org) = await ArrangeAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var consent = await SchemaData.InsertConsentAsync(owner, patient, org);

        var withConsent = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                "INSERT INTO audit_log (actor_kind, actor_id, patient_id, action, consent_grant_id) VALUES ('clinician', gen_random_uuid(), @patient, 'access_denied', @consent)",
                new { patient, consent }
            )
        );
        var withResults = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                "INSERT INTO audit_log (actor_kind, actor_id, patient_id, action, lab_result_ids) VALUES ('clinician', gen_random_uuid(), @patient, 'access_denied', ARRAY[gen_random_uuid()])",
                new { patient }
            )
        );

        Assert.Equal(PostgresErrorCodes.CheckViolation, withConsent.SqlState);
        Assert.Equal(PostgresErrorCodes.CheckViolation, withResults.SqlState);
    }

    [Fact]
    public async Task Two_people_of_the_same_kind_cannot_share_a_login_subject()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var subject = $"subject-{Guid.NewGuid():N}";
        const string insert =
            "INSERT INTO patients (full_name, date_of_birth, external_subject_id) VALUES ('Test Patient', '1990-01-01', @subject)";
        await owner.ExecuteAsync(insert, new { subject });

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(insert, new { subject })
        );

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
    }

    [Fact]
    public async Task Any_number_of_people_can_have_no_login()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        const string insert =
            "INSERT INTO patients (full_name, date_of_birth) VALUES ('Test Patient', '1990-01-01')";

        await owner.ExecuteAsync(insert);
        var inserted = await owner.ExecuteAsync(insert);

        Assert.Equal(1, inserted);
    }
}
