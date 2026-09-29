using Npgsql;
using PatientPortal.Migrations;
using Testcontainers.PostgreSql;

namespace PatientPortal.Api.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var result = MigrationRunner.Run(ConnectionString, includeDevSeed: false);
        if (!result.Successful)
        {
            throw new InvalidOperationException("Migrations failed.", result.Error);
        }
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
