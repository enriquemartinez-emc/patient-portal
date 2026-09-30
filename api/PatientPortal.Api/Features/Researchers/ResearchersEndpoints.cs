namespace PatientPortal.Api.Features.Researchers;

public static class ResearchersEndpoints
{
    public static IEndpointRouteBuilder MapResearchersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/researchers/{researcherId:guid}").WithTags("Researchers");

        group.MapListResearchParticipantsEndpoint();
        group.MapReadResearcherPatientLabResultsEndpoint();

        return app;
    }
}
