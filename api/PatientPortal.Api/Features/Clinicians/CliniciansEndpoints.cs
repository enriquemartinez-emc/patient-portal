using PatientPortal.Api.Infrastructure.Auth;

namespace PatientPortal.Api.Features.Clinicians;

public static class CliniciansEndpoints
{
    public static IEndpointRouteBuilder MapCliniciansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clinicians/{clinicianId:guid}")
            .WithTags("Clinicians")
            .RequireAuthorization(Policies.ClinicianActing);

        group.MapListMyPatientsEndpoint();
        group.MapGetClinicianPatientEndpoint();
        group.MapReadClinicianPatientLabResultsEndpoint();

        return app;
    }
}
