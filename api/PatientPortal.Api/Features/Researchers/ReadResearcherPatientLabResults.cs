using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.Features.Researchers;

public static class ReadResearcherPatientLabResultsEndpoint
{
    public static void MapReadResearcherPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ResearcherReadPatientLabResults");

    private static async Task<Results<Ok<ListLabResultsResponse>, ProblemHttpResult>> Handle(
        Guid researcherId,
        Guid patientId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var read = await PatientRecordReader.ReadAsResearcherAsync(
            connection,
            researcherId,
            patientId,
            time,
            ct
        );

        return read switch
        {
            RecordReadGranted granted => TypedResults.Ok(
                new ListLabResultsResponse([.. granted.Results.Select(row => row.ToResponse())])
            ),
            _ => TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access denied.",
                detail: "You are not allowed to view this patient's records."
            ),
        };
    }
}
