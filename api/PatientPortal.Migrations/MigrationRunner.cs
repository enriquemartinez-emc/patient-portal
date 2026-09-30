using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace PatientPortal.Migrations;

public static class MigrationRunner
{
    private const int SchemaRunGroup = 1;
    private const int DevSeedRunGroup = 2;

    public static DatabaseUpgradeResult Run(
        string connectionString,
        string appPassword,
        bool includeDevSeed
    )
    {
        // The password is substituted into a SQL string literal by 0003_app_role.sql.
        if (string.IsNullOrWhiteSpace(appPassword) || appPassword.Contains('\''))
        {
            throw new ArgumentException(
                "The application role password must be non-empty and must not contain a single quote.",
                nameof(appPassword)
            );
        }

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
            .WithVariable("AppPassword", appPassword)
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
