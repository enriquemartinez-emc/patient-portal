using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PostgresFixtureTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Fixture_provides_migrated_postgres_18()
    {
        await using var connection = await postgres.OpenConnectionAsync();

        await using var version = new NpgsqlCommand("SHOW server_version_num", connection);
        var versionNum = int.Parse((string)(await version.ExecuteScalarAsync())!);

        Assert.True(versionNum >= 180000, $"Expected Postgres 18+, got {versionNum}");
    }
}
