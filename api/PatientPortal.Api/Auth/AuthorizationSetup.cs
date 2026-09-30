using Microsoft.AspNetCore.Authorization;

namespace PatientPortal.Api.Auth;

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
        services.AddAuthorization(options =>
        {
            // Nothing is public unless it says so.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Roles decide which routes a caller may try; "acting as" ties them to the person in the URL.
            options.AddPolicy(
                Policies.PatientActing,
                policy =>
                    policy
                        .RequireRole("patient")
                        .AddRequirements(new ActingAsRequirement(ActorKind.Patient, "patientId"))
            );
            options.AddPolicy(
                Policies.ClinicianActing,
                policy =>
                    policy
                        .RequireRole("clinician")
                        .AddRequirements(
                            new ActingAsRequirement(ActorKind.Clinician, "clinicianId")
                        )
            );
            options.AddPolicy(
                Policies.ResearcherActing,
                policy =>
                    policy
                        .RequireRole("researcher")
                        .AddRequirements(
                            new ActingAsRequirement(ActorKind.Researcher, "researcherId")
                        )
            );
            options.AddPolicy(Policies.PatientOnly, policy => policy.RequireRole("patient"));
            options.AddPolicy(Policies.AdminOnly, policy => policy.RequireRole("admin"));
        });

        services.AddSingleton<IAuthorizationHandler, ActingAsHandler>();
        services.AddSingleton<IAuthorizationHandler, PatientRecordAccessHandler>();

        return services;
    }
}
