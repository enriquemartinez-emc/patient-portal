using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class DevSeedTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Dev_seed_loads_the_sample_data_on_top_of_the_schema()
    {
        var connectionString = await postgres.CreateMigratedDatabaseAsync(includeDevSeed: true);
        await using var connection = new NpgsqlConnection(connectionString);

        var counts = await connection.QuerySingleAsync<Counts>(
            """
            SELECT
                (SELECT count(*) FROM organizations)::int AS Organizations,
                (SELECT count(*) FROM patients)::int AS Patients,
                (SELECT count(*) FROM clinicians)::int AS Clinicians,
                (SELECT count(*) FROM researchers)::int AS Researchers,
                (SELECT count(*) FROM treatment_relationships)::int AS Treatments,
                (SELECT count(*) FROM treatment_relationships WHERE ended_at IS NULL)::int AS ActiveTreatments,
                (SELECT count(*) FROM consent_grants)::int AS Consents,
                (SELECT count(*) FROM lab_results)::int AS LabResults,
                (SELECT count(*) FROM audit_log)::int AS AuditEntries,
                (SELECT count(*) FROM patients WHERE external_subject_id IS NULL)::int
                    + (SELECT count(*) FROM clinicians WHERE external_subject_id IS NULL)::int
                    + (SELECT count(*) FROM researchers WHERE external_subject_id IS NULL)::int AS PeopleWithoutLogin
            """
        );

        Assert.Equal(new Counts(3, 2, 2, 1, 3, 2, 2, 45, 0, 0), counts);
    }

    [Fact]
    public async Task The_shared_test_database_is_migrated_without_seed_data()
    {
        await using var connection = await postgres.OpenOwnerConnectionAsync();

        var seeded = await connection.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM organizations WHERE id = 'a0000000-0000-0000-0000-000000000001'"
        );

        Assert.Equal(0, seeded);
    }

    private sealed record Counts(
        int Organizations,
        int Patients,
        int Clinicians,
        int Researchers,
        int Treatments,
        int ActiveTreatments,
        int Consents,
        int LabResults,
        int AuditEntries,
        int PeopleWithoutLogin
    );
}
