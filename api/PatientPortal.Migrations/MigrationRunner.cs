using System.Reflection;
using DbUp;
using DbUp.Engine;

namespace PatientPortal.Migrations;

public static class MigrationRunner
{
    public static DatabaseUpgradeResult Run(string connectionString, bool includeDevSeed)
    {
        var assembly = Assembly.GetExecutingAssembly();

        var upgrader = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                assembly,
                name => name.Contains(".Scripts.Schema.", StringComparison.Ordinal)
            )
            .WithScriptsEmbeddedInAssembly(
                assembly,
                name =>
                    includeDevSeed && name.Contains(".Scripts.DevSeed.", StringComparison.Ordinal)
            )
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
