using Dapper;
using Npgsql;

namespace PatientPortal.Api.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class WebRolePrivilegeTests(PostgresFixture postgres)
{
    [Fact]
    public async Task The_web_role_can_manage_sessions_and_linked_accounts()
    {
        await using var web = await postgres.OpenWebConnectionAsync();
        var user = Guid.NewGuid().ToString("N");

        await web.ExecuteAsync(
            "INSERT INTO web_users (id, name, email) VALUES (@user, 'Test User', @email)",
            new { user, email = $"{user}@demo.example" }
        );
        await web.ExecuteAsync(
            "INSERT INTO web_sessions (id, expires_at, token, updated_at, user_id) VALUES (@id, now() + interval '1 day', @id, now(), @user)",
            new { id = Guid.NewGuid().ToString("N"), user }
        );
        await web.ExecuteAsync(
            "INSERT INTO web_accounts (id, account_id, provider_id, user_id, access_token, updated_at) VALUES (@id, 'sub', 'keycloak', @user, 'encrypted', now())",
            new { id = Guid.NewGuid().ToString("N"), user }
        );
        var refreshed = await web.ExecuteAsync(
            "UPDATE web_accounts SET access_token = 'newer' WHERE user_id = @user",
            new { user }
        );
        var deleted = await web.ExecuteAsync(
            "DELETE FROM web_sessions WHERE user_id = @user",
            new { user }
        );

        Assert.Equal(1, refreshed);
        Assert.Equal(1, deleted);
    }

    [Theory]
    [InlineData("SELECT * FROM patients")]
    [InlineData("SELECT * FROM lab_results")]
    [InlineData("SELECT * FROM consent_grants")]
    [InlineData("SELECT * FROM audit_log")]
    [InlineData(
        "INSERT INTO audit_log (actor_kind, actor_id, patient_id, action) VALUES ('patient', gen_random_uuid(), gen_random_uuid(), 'access_denied')"
    )]
    [InlineData("CREATE TABLE web_created (id int)")]
    public async Task The_web_role_cannot_touch_anything_outside_its_own_tables(string sql)
    {
        await using var web = await postgres.OpenWebConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() => web.ExecuteAsync(sql));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Theory]
    [InlineData("web_users")]
    [InlineData("web_sessions")]
    [InlineData("web_accounts")]
    [InlineData("web_verifications")]
    public async Task The_api_role_cannot_see_the_web_sign_in_tables(string table)
    {
        await using var app = await postgres.OpenAppConnectionAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            app.ExecuteAsync($"SELECT * FROM {table}")
        );

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }
}
