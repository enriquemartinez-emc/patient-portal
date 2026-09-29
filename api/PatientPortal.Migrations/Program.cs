using PatientPortal.Migrations;

var connectionString = Environment.GetEnvironmentVariable("MIGRATIONS_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("MIGRATIONS_CONNECTION_STRING is not set.");
    return 1;
}

var includeDevSeed = string.Equals(
    Environment.GetEnvironmentVariable("MIGRATIONS_INCLUDE_DEV_SEED"),
    "true",
    StringComparison.OrdinalIgnoreCase
);

var result = MigrationRunner.Run(connectionString, includeDevSeed);
if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

return 0;
