using PatientPortal.Migrations;

var connectionString = Environment.GetEnvironmentVariable("MIGRATIONS_CONNECTION_STRING");
var appPassword = Environment.GetEnvironmentVariable("PP_APP_PASSWORD");
if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(appPassword))
{
    Console.Error.WriteLine("MIGRATIONS_CONNECTION_STRING and PP_APP_PASSWORD must be set.");
    return 1;
}

var includeDevSeed = string.Equals(
    Environment.GetEnvironmentVariable("MIGRATIONS_INCLUDE_DEV_SEED"),
    "true",
    StringComparison.OrdinalIgnoreCase
);

var result = MigrationRunner.Run(connectionString, appPassword, includeDevSeed);
if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

return 0;
