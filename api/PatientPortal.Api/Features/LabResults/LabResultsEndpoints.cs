using PatientPortal.Api.Infrastructure.Auth;

namespace PatientPortal.Api.Features.LabResults;

public static class LabResultsEndpoints
{
    public static IEndpointRouteBuilder MapLabResultsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/patients/{patientId:guid}/lab-results")
            .WithTags("Lab results")
            .RequireAuthorization(Policies.PatientActing);

        group.MapListLabResultsEndpoint();
        group.MapGetLabResultEndpoint();

        return app;
    }
}
