using System.Reflection;
using DbUp;
using DbUp.Engine;
using Npgsql;

namespace PatientPortal.Migrations;

public static class MigrationRunner
{
    private const int SchemaRunGroup = 1;
    private const int DevSeedRunGroup = 2;

    // Migrations must use a direct, non-pooled connection: the password step relies on session
    // state.
    public static DatabaseUpgradeResult Run(
        string connectionString,
        string? appRolePassword,
        bool includeDevSeed,
        string? webRolePassword = null
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
        if (result.Successful)
        {
            if (!string.IsNullOrEmpty(appRolePassword))
            {
                SetRolePassword(connectionString, "patient_portal_app", appRolePassword);
            }
            if (!string.IsNullOrEmpty(webRolePassword))
            {
                SetRolePassword(connectionString, "patient_portal_web", webRolePassword);
            }
        }

        return result;
    }

    // ALTER ROLE cannot take bind parameters, so the role and password travel as session settings and
    // are quoted server-side with format('%I') and format('%L'). Neither appears in a SQL string built
    // by this program.
    private static void SetRolePassword(string connectionString, string role, string password)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using (
            var stash = new NpgsqlCommand(
                "SELECT set_config('migrations.role', @role, false), set_config('migrations.password', @password, false)",
                connection
            )
        )
        {
            stash.Parameters.AddWithValue("role", role);
            stash.Parameters.AddWithValue("password", password);
            stash.ExecuteNonQuery();
        }

        using var alter = new NpgsqlCommand(
            """
            DO $$
            BEGIN
                EXECUTE format('ALTER ROLE %I PASSWORD %L', current_setting('migrations.role'), current_setting('migrations.password'));
            END;
            $$
            """,
            connection
        );
        alter.ExecuteNonQuery();

        using var clear = new NpgsqlCommand(
            "RESET migrations.role; RESET migrations.password",
            connection
        );
        clear.ExecuteNonQuery();
    }
}
