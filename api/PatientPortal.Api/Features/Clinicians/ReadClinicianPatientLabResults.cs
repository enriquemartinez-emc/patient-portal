using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Api.Features.LabResults;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Clinicians;

public static class ReadClinicianPatientLabResultsEndpoint
{
    public static void MapReadClinicianPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ClinicianReadPatientLabResults");

    // A treating clinician sees every category. Otherwise the clinician sees only what the
    // patient's consents to the clinician's organization cover. Whether the clinician may make the
    // request at all is decided by authorization (Phase 6), not here. The read and its audit entry
    // commit together, so no results are returned unless the read was recorded.
    private static async Task<Results<Ok<ListLabResultsResponse>, ProblemHttpResult>> Handle(
        Guid clinicianId,
        Guid patientId,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var organizationId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "select organization_id from clinicians where id = @clinicianId",
                new { clinicianId },
                transaction,
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

        if (!await PatientGuard.ExistsAsync(connection, patientId, ct, transaction))
        {
            return PatientGuard.NotFound(patientId);
        }

        var now = time.GetUtcNow();

        // FOR SHARE keeps a concurrent "end treatment" from committing mid-read.
        var treating = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                """
                select id from treatment_relationships
                where patient_id = @patientId and clinician_id = @clinicianId and ended_at is null
                for share
                """,
                new { patientId, clinicianId },
                transaction,
                cancellationToken: ct
            )
        );

        var categories = treating is not null
            ? LabCategoryNames.All
            : await ConsentCoverage.CoveredCategoriesAsync(
                connection,
                transaction,
                patientId,
                organizationId.Value,
                now,
                ct
            );

        var rows = await LabResultsQuery.InCategoriesAsync(
            connection,
            transaction,
            patientId,
            categories,
            ct
        );

        var patient = new PatientId(patientId);
        var audit = AuditEntries.ForLabResultsRead(
            new AuditLogEntryId(Guid.CreateVersion7()),
            now,
            new ClinicianActor(new ClinicianId(clinicianId)),
            patient,
            [.. rows.Select(row => row.ToLabResult(patient))]
        );
        await AuditLogWriter.InsertAsync(connection, transaction, audit, ct);

        await transaction.CommitAsync(ct);

        return TypedResults.Ok(
            new ListLabResultsResponse([.. rows.Select(row => row.ToResponse())])
        );
    }
}
