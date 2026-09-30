using Npgsql;
using PatientPortal.Migrations;
using Testcontainers.PostgreSql;

namespace PatientPortal.Api.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    public const string AppRole = "patient_portal_app";
    public const string AppPassword = "integration-tests-app-password";
    public const string WebRole = "patient_portal_web";
    public const string WebPassword = "integration-tests-web-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    // Connects as the migration user, which owns the schema.
    public string OwnerConnectionString => _container.GetConnectionString();

    public string AppConnectionString =>
        new NpgsqlConnectionStringBuilder(OwnerConnectionString)
        {
            Username = AppRole,
            Password = AppPassword,
        }.ConnectionString;

    public string WebConnectionString =>
        new NpgsqlConnectionStringBuilder(OwnerConnectionString)
        {
            Username = WebRole,
            Password = WebPassword,
        }.ConnectionString;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        RunMigrations(OwnerConnectionString, includeDevSeed: false);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public Task<NpgsqlConnection> OpenOwnerConnectionAsync() => OpenAsync(OwnerConnectionString);

    public Task<NpgsqlConnection> OpenAppConnectionAsync() => OpenAsync(AppConnectionString);

    public Task<NpgsqlConnection> OpenWebConnectionAsync() => OpenAsync(WebConnectionString);

    // Creates an empty database in the same container and migrates it.
    public async Task<string> CreateMigratedDatabaseAsync(bool includeDevSeed)
    {
        var name = $"db_{Guid.NewGuid():N}";
        await using (var owner = await OpenOwnerConnectionAsync())
        {
            await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", owner);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(OwnerConnectionString)
        {
            Database = name,
            Pooling = false,
        }.ConnectionString;
        RunMigrations(connectionString, includeDevSeed);
        return connectionString;
    }

    private static void RunMigrations(string connectionString, bool includeDevSeed)
    {
        var result = MigrationRunner.Run(
            connectionString,
            AppPassword,
            includeDevSeed,
            WebPassword
        );
        if (!result.Successful)
        {
            throw new InvalidOperationException("Migrations failed.", result.Error);
        }
    }

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
