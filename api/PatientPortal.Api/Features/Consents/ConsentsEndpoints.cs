using PatientPortal.Api.Infrastructure.Auth;

namespace PatientPortal.Api.Features.Consents;

public static class ConsentsEndpoints
{
    public static IEndpointRouteBuilder MapConsentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/patients/{patientId:guid}/consents")
            .WithTags("Consents")
            .RequireAuthorization(Policies.PatientActing);

        group.MapGrantConsentEndpoint();
        group.MapRevokeConsentEndpoint();
        group.MapListConsentsEndpoint();
        group.MapGetConsentEndpoint();

        return app;
    }
}
