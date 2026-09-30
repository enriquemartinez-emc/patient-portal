namespace PatientPortal.Api.Features.Treatment;

public static class TreatmentEndpoints
{
    public static IEndpointRouteBuilder MapTreatmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/treatment-relationships").WithTags("Treatment relationships");

        group.MapStartTreatmentEndpoint();
        group.MapEndTreatmentEndpoint();
        group.MapGetTreatmentEndpoint();

        return app;
    }
}
