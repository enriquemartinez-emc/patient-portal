using PatientPortal.Migrations;

var connectionString = Environment.GetEnvironmentVariable("MIGRATIONS_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("MIGRATIONS_CONNECTION_STRING must be set.");
    return 1;
}

// Optional: when unset, the application role keeps whatever password it already has.
var appRolePassword = Environment.GetEnvironmentVariable("APP_ROLE_PASSWORD");

var includeDevSeed = string.Equals(
    Environment.GetEnvironmentVariable("MIGRATIONS_INCLUDE_DEV_SEED"),
    "true",
    StringComparison.OrdinalIgnoreCase
);

var result = MigrationRunner.Run(connectionString, appRolePassword, includeDevSeed);
if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

return 0;
