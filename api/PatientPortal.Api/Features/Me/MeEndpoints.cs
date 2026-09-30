namespace PatientPortal.Api.Features.Me;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/me").WithTags("Me");

        group.MapGetMeEndpoint();

        return app;
    }
}
