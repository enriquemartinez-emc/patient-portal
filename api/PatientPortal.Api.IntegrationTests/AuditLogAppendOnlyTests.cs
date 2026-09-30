using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AuditLogAppendOnlyTests(PostgresFixture postgres)
{
    private const string InsufficientPrivilege = PostgresErrorCodes.InsufficientPrivilege;

    private async Task<Guid> ArrangeAuditEntryAsync()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);
        return await SchemaData.InsertAuditEntryAsync(owner, patient, Guid.NewGuid());
    }

    [Fact]
    public async Task App_role_can_append_and_read_audit_entries()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        var patient = await SchemaData.InsertPatientAsync(owner);

        await using var app = await postgres.OpenAppConnectionAsync();
        var id = await SchemaData.InsertAuditEntryAsync(app, patient, Guid.NewGuid());

        var count = await app.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM audit_log WHERE id = @id",
            new { id }
        );
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task App_role_cannot_update_audit_entries()
    {
        var id = await ArrangeAuditEntryAsync();
        await using var app = await postgres.OpenAppConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            app.ExecuteAsync(
                "UPDATE audit_log SET actor_kind = 'patient' WHERE id = @id",
                new { id }
            )
        );

        Assert.Equal(InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_role_cannot_delete_audit_entries()
    {
        var id = await ArrangeAuditEntryAsync();
        await using var app = await postgres.OpenAppConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            app.ExecuteAsync("DELETE FROM audit_log WHERE id = @id", new { id })
        );

        Assert.Equal(InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_role_cannot_truncate_the_audit_log()
    {
        await using var app = await postgres.OpenAppConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            app.ExecuteAsync("TRUNCATE audit_log")
        );

        Assert.Equal(InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task Trigger_blocks_update_and_delete_even_for_the_schema_owner()
    {
        var id = await ArrangeAuditEntryAsync();
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync(
                "UPDATE audit_log SET actor_kind = 'patient' WHERE id = @id",
                new { id }
            )
        );
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync("DELETE FROM audit_log WHERE id = @id", new { id })
        );

        Assert.Contains("append-only", update.MessageText);
        Assert.Contains("append-only", delete.MessageText);
    }

    [Fact]
    public async Task Trigger_blocks_truncate_even_for_the_schema_owner()
    {
        await using var owner = await postgres.OpenOwnerConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            owner.ExecuteAsync("TRUNCATE audit_log")
        );

        Assert.Contains("append-only", error.MessageText);
    }

    [Fact]
    public async Task Trigger_blocks_a_role_that_has_been_granted_update_delete_and_truncate()
    {
        var id = await ArrangeAuditEntryAsync();
        var role = $"granted_{Guid.NewGuid():N}";
        await using var owner = await postgres.OpenOwnerConnectionAsync();
        await owner.ExecuteAsync(
            $"""
            CREATE ROLE {role} LOGIN PASSWORD 'irrelevant';
            GRANT SELECT, UPDATE, DELETE, TRUNCATE ON audit_log TO {role};
            """
        );

        var connectionString = new NpgsqlConnectionStringBuilder(postgres.OwnerConnectionString)
        {
            Username = role,
            Password = "irrelevant",
            Pooling = false,
        }.ConnectionString;
        await using var granted = new NpgsqlConnection(connectionString);
        await granted.OpenAsync();

        var update = await Assert.ThrowsAsync<PostgresException>(() =>
            granted.ExecuteAsync(
                "UPDATE audit_log SET actor_kind = 'patient' WHERE id = @id",
                new { id }
            )
        );
        var delete = await Assert.ThrowsAsync<PostgresException>(() =>
            granted.ExecuteAsync("DELETE FROM audit_log WHERE id = @id", new { id })
        );
        var truncate = await Assert.ThrowsAsync<PostgresException>(() =>
            granted.ExecuteAsync("TRUNCATE audit_log")
        );

        Assert.Contains("append-only", update.MessageText);
        Assert.Contains("append-only", delete.MessageText);
        Assert.Contains("append-only", truncate.MessageText);
    }
}
