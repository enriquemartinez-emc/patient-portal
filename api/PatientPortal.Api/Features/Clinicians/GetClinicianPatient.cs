using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace PatientPortal.Api.Features.Clinicians;

public static class GetClinicianPatientEndpoint
{
    public static void MapGetClinicianPatientEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}", Handle).WithName("GetClinicianPatient");

    // One of the clinician's patients, with the basis for the relationship. A patient the clinician
    // neither treats nor has a consent for is not one of their patients, so it is not found.
    private static async Task<Results<Ok<PatientSummaryResponse>, ProblemHttpResult>> Handle(
        Guid clinicianId,
        Guid patientId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var organizationId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "select organization_id from clinicians where id = @clinicianId",
                new { clinicianId },
                cancellationToken: ct
            )
        );
        if (organizationId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Clinician not found.",
                detail: $"Clinician '{clinicianId}' does not exist."
            );
        }

        const string sql = """
            select p.id as Id, p.full_name as FullName, p.date_of_birth as DateOfBirth,
                   exists (
                       select 1 from treatment_relationships t
                       where t.patient_id = p.id and t.clinician_id = @clinicianId and t.ended_at is null
                   ) as HasTreatment,
                   exists (
                       select 1 from consent_grants g
                       where g.patient_id = p.id
                         and g.grantee_organization_id = @organizationId
                         and g.revoked_at is null
                         and (g.expires_at is null or g.expires_at > @now)
                   ) as HasConsent
            from patients p
            where p.id = @patientId
            """;

        var row = await connection.QuerySingleOrDefaultAsync<DbPatientRow>(
            new CommandDefinition(
                sql,
                new
                {
                    clinicianId,
                    organizationId,
                    patientId,
                    now = time.GetUtcNow(),
                },
                cancellationToken: ct
            )
        );

        if (row is null || (!row.HasTreatment && !row.HasConsent))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Patient not found.",
                detail: $"Clinician '{clinicianId}' has no patient '{patientId}'."
            );
        }

        return TypedResults.Ok(row.ToResponse());
    }
}
