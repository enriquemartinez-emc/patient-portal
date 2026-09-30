namespace PatientPortal.Api.Features.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/patients/{patientId:guid}/audit").WithTags("Audit");

        group.MapListAuditTrailEndpoint();

        return app;
    }
}
