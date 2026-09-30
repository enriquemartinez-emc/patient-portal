using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.Features.Clinicians;

public static class ReadClinicianPatientLabResultsEndpoint
{
    public static void MapReadClinicianPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ClinicianReadPatientLabResults");

    private static async Task<Results<Ok<ListLabResultsResponse>, ProblemHttpResult>> Handle(
        Guid clinicianId,
        Guid patientId,
        ClaimsPrincipal user,
        IAuthorizationService authorization,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var read = await PatientRecordReader.ReadAsClinicianAsync(
            connection,
            user,
            authorization,
            clinicianId,
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
