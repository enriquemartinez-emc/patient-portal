using System.Reflection;
using DbUp;
using DbUp.Engine;
using Npgsql;

namespace PatientPortal.Migrations;

public static class MigrationRunner
{
    private const int SchemaRunGroup = 1;
    private const int DevSeedRunGroup = 2;

    // Runs the versioned scripts, then (when a password is given) sets the application role's
    // password. Migrations must use a direct, non-pooled connection: the password step relies
    // on session state.
    public static DatabaseUpgradeResult Run(
        string connectionString,
        string? appRolePassword,
        bool includeDevSeed
    )
    {
        var assembly = Assembly.GetExecutingAssembly();

        var upgrader = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                assembly,
                name => name.Contains(".Scripts.Schema.", StringComparison.Ordinal),
                new SqlScriptOptions { RunGroupOrder = SchemaRunGroup }
            )
            .WithScriptsEmbeddedInAssembly(
                assembly,
                name =>
                    includeDevSeed && name.Contains(".Scripts.DevSeed.", StringComparison.Ordinal),
                new SqlScriptOptions { RunGroupOrder = DevSeedRunGroup }
            )
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (result.Successful && !string.IsNullOrEmpty(appRolePassword))
        {
            SetAppRolePassword(connectionString, appRolePassword);
        }

        return result;
    }

    // ALTER ROLE cannot take bind parameters, so the password travels as a session setting and is
    // quoted server-side with format('%L'). It never appears in a SQL string built by this program.
    private static void SetAppRolePassword(string connectionString, string password)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using (
            var stash = new NpgsqlCommand(
                "SELECT set_config('migrations.app_role_password', @password, false)",
                connection
            )
        )
        {
            stash.Parameters.AddWithValue("password", password);
            stash.ExecuteNonQuery();
        }

        using var alter = new NpgsqlCommand(
            """
            DO $$
            BEGIN
                EXECUTE format('ALTER ROLE patient_portal_app PASSWORD %L', current_setting('migrations.app_role_password'));
            END;
            $$
            """,
            connection
        );
        alter.ExecuteNonQuery();

        using var clear = new NpgsqlCommand("RESET migrations.app_role_password", connection);
        clear.ExecuteNonQuery();
    }
}
