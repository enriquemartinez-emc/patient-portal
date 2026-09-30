using Npgsql;
using PatientPortal.Migrations;
using Testcontainers.PostgreSql;

namespace PatientPortal.Api.IntegrationTests;

// Roles are cluster-wide, so these tests get their own container rather than the shared fixture.
public sealed class AppRolePasswordTests
{
    private static async Task<PostgreSqlContainer> StartAsync()
    {
        var container = new PostgreSqlBuilder("postgres:18").Build();
        await container.StartAsync();
        return container;
    }

    private static async Task LoginAsync(
        string ownerConnectionString,
        string password,
        string role = PostgresFixture.AppRole
    )
    {
        var connectionString = new NpgsqlConnectionStringBuilder(ownerConnectionString)
        {
            Username = role,
            Password = password,
            Pooling = false,
        }.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
    }

    [Fact]
    public async Task Without_a_password_the_app_role_exists_but_cannot_log_in()
    {
        await using var container = await StartAsync();
        var connectionString = container.GetConnectionString();

        var result = MigrationRunner.Run(
            connectionString,
            appRolePassword: null,
            includeDevSeed: false
        );

        Assert.True(result.Successful);
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            LoginAsync(connectionString, "any password")
        );
        Assert.Equal(PostgresErrorCodes.InvalidPassword, error.SqlState);
    }

    [Fact]
    public async Task The_supplied_password_is_set_even_when_it_contains_quotes()
    {
        await using var container = await StartAsync();
        var connectionString = container.GetConnectionString();
        const string password = "it's a \"quoted\" pass;word=1";

        var result = MigrationRunner.Run(connectionString, password, includeDevSeed: false);

        Assert.True(result.Successful);
        await LoginAsync(connectionString, password);
    }

    [Fact]
    public async Task Running_migrations_again_with_a_new_password_rotates_it()
    {
        await using var container = await StartAsync();
        var connectionString = container.GetConnectionString();
        MigrationRunner.Run(connectionString, "first-password", includeDevSeed: false);

        var result = MigrationRunner.Run(
            connectionString,
            "second-password",
            includeDevSeed: false
        );

        Assert.True(result.Successful);
        var old = await Assert.ThrowsAsync<PostgresException>(() =>
            LoginAsync(connectionString, "first-password")
        );
        Assert.Equal(PostgresErrorCodes.InvalidPassword, old.SqlState);
        await LoginAsync(connectionString, "second-password");
    }

    [Fact]
    public async Task Running_migrations_again_without_a_password_keeps_the_existing_one()
    {
        await using var container = await StartAsync();
        var connectionString = container.GetConnectionString();
        MigrationRunner.Run(connectionString, "kept-password", includeDevSeed: false);

        var result = MigrationRunner.Run(
            connectionString,
            appRolePassword: null,
            includeDevSeed: false
        );

        Assert.True(result.Successful);
        await LoginAsync(connectionString, "kept-password");
    }

    [Fact]
    public async Task The_web_role_gets_its_own_password_independent_of_the_app_role()
    {
        await using var container = await StartAsync();
        var connectionString = container.GetConnectionString();

        var result = MigrationRunner.Run(
            connectionString,
            appRolePassword: "app-password",
            includeDevSeed: false,
            webRolePassword: "web-password"
        );

        Assert.True(result.Successful);
        await LoginAsync(connectionString, "app-password", PostgresFixture.AppRole);
        await LoginAsync(connectionString, "web-password", PostgresFixture.WebRole);
        var mixedUp = await Assert.ThrowsAsync<PostgresException>(() =>
            LoginAsync(connectionString, "app-password", PostgresFixture.WebRole)
        );
        Assert.Equal(PostgresErrorCodes.InvalidPassword, mixedUp.SqlState);
    }
}
