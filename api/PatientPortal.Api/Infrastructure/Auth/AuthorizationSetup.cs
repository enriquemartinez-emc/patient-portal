using Microsoft.AspNetCore.Authorization;

namespace PatientPortal.Api.Infrastructure.Auth;

public static class Policies
{
    public const string PatientActing = nameof(PatientActing);
    public const string ClinicianActing = nameof(ClinicianActing);
    public const string ResearcherActing = nameof(ResearcherActing);
    public const string PatientOnly = nameof(PatientOnly);
    public const string AdminOnly = nameof(AdminOnly);
}

public static class AuthorizationSetup
{
    public static IServiceCollection AddPortalAuthorization(this IServiceCollection services)
    {
        services
            .AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(
                Policies.PatientActing,
                policy =>
                    policy
                        .RequireRole("patient")
                        .AddRequirements(new RouteActorRequirement(ActorKind.Patient, "patientId"))
            )
            .AddPolicy(
                Policies.ClinicianActing,
                policy =>
                    policy
                        .RequireRole("clinician")
                        .AddRequirements(
                            new RouteActorRequirement(ActorKind.Clinician, "clinicianId")
                        )
            )
            .AddPolicy(
                Policies.ResearcherActing,
                policy =>
                    policy
                        .RequireRole("researcher")
                        .AddRequirements(
                            new RouteActorRequirement(ActorKind.Researcher, "researcherId")
                        )
            )
            .AddPolicy(Policies.PatientOnly, policy => policy.RequireRole("patient"))
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole("admin"));

        services.AddSingleton<IAuthorizationHandler, RouteActorHandler>();

        return services;
    }
}
