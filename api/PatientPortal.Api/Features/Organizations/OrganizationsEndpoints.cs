using PatientPortal.Api.Auth;

namespace PatientPortal.Api.Features.Organizations;

public static class OrganizationsEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organizations")
            .WithTags("Organizations")
            .RequireAuthorization(Policies.PatientOnly);

        group.MapListOrganizationsEndpoint();

        return app;
    }
}
