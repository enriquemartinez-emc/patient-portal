namespace PatientPortal.Api.Features.Clinicians;

public static class CliniciansEndpoints
{
    public static IEndpointRouteBuilder MapCliniciansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clinicians/{clinicianId:guid}").WithTags("Clinicians");

        group.MapListMyPatientsEndpoint();
        group.MapGetClinicianPatientEndpoint();
        group.MapReadClinicianPatientLabResultsEndpoint();

        return app;
    }
}
