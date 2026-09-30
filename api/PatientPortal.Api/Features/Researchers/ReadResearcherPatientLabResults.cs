using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Api.Features.LabResults;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Researchers;

public static class ReadResearcherPatientLabResultsEndpoint
{
    public static void MapReadResearcherPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ResearcherReadPatientLabResults");

    // Researchers never have a treatment relationship: they see only the categories the patient's
    // consents to the researcher's organization cover (data minimization), and every read is audited.
    private static async Task<Results<Ok<ListLabResultsResponse>, ProblemHttpResult>> Handle(
        Guid researcherId,
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
                "select organization_id from researchers where id = @researcherId",
                new { researcherId },
                transaction,
                cancellationToken: ct
            )
        );
        if (organizationId is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Researcher not found.",
                detail: $"Researcher '{researcherId}' does not exist."
            );
        }

        if (!await PatientGuard.ExistsAsync(connection, patientId, ct, transaction))
        {
            return PatientGuard.NotFound(patientId);
        }

        var now = time.GetUtcNow();

        var categories = await ConsentCoverage.CoveredCategoriesAsync(
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
            new ResearcherActor(new ResearcherId(researcherId)),
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
