using Npgsql;

namespace PatientPortal.Api.Infrastructure.Database;

public static class DatabaseSetup
{
    // One shared data source for the whole app. Dapper's type handlers are process-wide, so they are
    // registered here once, before the first query runs.
    public static IServiceCollection AddPortalDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        DapperConfiguration.Register();

        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        return services;
    }
}
