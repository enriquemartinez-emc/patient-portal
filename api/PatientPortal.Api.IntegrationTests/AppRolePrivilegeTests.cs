using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AppRolePrivilegeTests(PostgresFixture postgres)
{
    [Fact]
    public async Task App_role_can_revoke_a_consent_but_not_edit_its_other_columns()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var consent = await SchemaData.InsertConsentAsync(owner, patient, org);
        await using var app = await postgres.OpenAppConnectionAsync();

        var revoked = await app.ExecuteAsync(
            "UPDATE consent_grants SET revoked_at = now() WHERE id = @consent",
            new { consent }
        );
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            app.ExecuteAsync(
                "UPDATE consent_grants SET categories = ARRAY['lipids'] WHERE id = @consent",
                new { consent }
            )
        );

        Assert.Equal(1, revoked);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_role_can_end_a_treatment_relationship_but_not_reassign_it()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        var org = await SchemaData.InsertOrganizationAsync(owner);
        var clinician = await SchemaData.InsertClinicianAsync(owner, org);
        await using var app = await postgres.OpenAppConnectionAsync();
        var relationship = await app.ExecuteScalarAsync<Guid>(
            """
            INSERT INTO treatment_relationships (patient_id, clinician_id, started_at)
            VALUES (@patient, @clinician, now() - interval '1 day') RETURNING id
            """,
            new { patient, clinician }
        );

        var ended = await app.ExecuteAsync(
            "UPDATE treatment_relationships SET ended_at = now() WHERE id = @relationship",
            new { relationship }
        );
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            app.ExecuteAsync(
                "UPDATE treatment_relationships SET patient_id = @other WHERE id = @relationship",
                new { other = Guid.NewGuid(), relationship }
            )
        );

        Assert.Equal(1, ended);
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Theory]
    [InlineData("DELETE FROM consent_grants")]
    [InlineData("DELETE FROM treatment_relationships")]
    [InlineData("DELETE FROM lab_results")]
    [InlineData("DELETE FROM patients")]
    [InlineData(
        "INSERT INTO lab_results (patient_id, category, test_name, value_amount, value_unit, collected_at) VALUES (gen_random_uuid(), 'lipids', 'x', 1, 'u', now())"
    )]
    [InlineData("INSERT INTO patients (full_name, date_of_birth) VALUES ('x', '2000-01-01')")]
    [InlineData("CREATE TABLE app_created (id int)")]
    public async Task App_role_is_denied_statements_outside_its_grants(string sql)
    {
        await using var app = await postgres.OpenAppConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() => app.ExecuteAsync(sql));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }
}
