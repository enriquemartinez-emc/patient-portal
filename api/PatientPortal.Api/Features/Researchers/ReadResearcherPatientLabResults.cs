using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Auth;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.LabResults;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Researchers;

public static class ReadResearcherPatientLabResultsEndpoint
{
    public static void MapReadResearcherPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ResearcherReadPatientLabResults");

    // Researchers never have a treatment relationship: they see only the categories the patient's
    // consents to the researcher's organization cover (data minimization). Reads and refusals are
    // both recorded in the patient's audit trail.
    private static async Task<Results<Ok<ListLabResultsResponse>, ProblemHttpResult>> Handle(
        Guid researcherId,
        Guid patientId,
        ClaimsPrincipal user,
        IAuthorizationService authorization,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var organizationId = await connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                "select organization_id from researchers where id = @researcherId",
                new { researcherId },
                transaction,
                cancellationToken: ct
            )
        );

        var request = new PatientRecordRequest(
            ActorKind.Researcher,
            organizationId,
            researcherId,
            patientId,
            connection,
            transaction,
            ct
        );
        var decision = await authorization.AuthorizeAsync(
            user,
            request,
            PatientRecordAccessRequirement.Instance
        );
        var actor = new ResearcherActor(new ResearcherId(researcherId));
        if (!decision.Succeeded)
        {
            return await AccessDenial.RefuseAsync(
                connection,
                transaction,
                actor,
                patientId,
                time,
                ct
            );
        }

        var rows = await LabResultsQuery.InCategoriesAsync(
            connection,
            transaction,
            patientId,
            request.VisibleCategories,
            ct
        );

        var patient = new PatientId(patientId);
        var audit = AuditEntries.ForLabResultsRead(
            new AuditLogEntryId(Guid.CreateVersion7()),
            time.GetUtcNow(),
            actor,
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
