using Npgsql;

namespace PatientPortal.Api.Infrastructure.Database;

public static class DatabaseSetup
{
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
