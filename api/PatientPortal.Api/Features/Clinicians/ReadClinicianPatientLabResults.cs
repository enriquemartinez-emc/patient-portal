using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.LabResults;

namespace PatientPortal.Api.Features.Clinicians;

public static class ReadClinicianPatientLabResultsEndpoint
{
    public static void MapReadClinicianPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ClinicianReadPatientLabResults");

    // A treating clinician sees every category; anyone else sees what the patient's consents to the
    // clinician's organization cover. The audited read itself is shared with the researcher endpoint.
    private static async Task<Results<Ok<ListLabResultsResponse>, ProblemHttpResult>> Handle(
        Guid clinicianId,
        Guid patientId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        var organizationId = await OrganizationLookup.OfClinicianAsync(connection, clinicianId, ct);

        var read = await PatientRecordReader.ReadAsClinicianAsync(
            connection,
            clinicianId,
            organizationId,
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
