using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using PatientPortal.Api.Auth;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.LabResults;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Clinicians;

public static class ReadClinicianPatientLabResultsEndpoint
{
    public static void MapReadClinicianPatientLabResultsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/patients/{patientId:guid}/lab-results", Handle)
            .WithName("ClinicianReadPatientLabResults");

    // The record-access check runs inside this transaction, so the treatment and consent rows it
    // relies on stay locked until the read commits. A refusal is recorded in the patient's audit
    // trail; a read is recorded together with its results, so nothing is returned unrecorded.
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
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var organizationId = await connection.QuerySingleAsync<Guid>(
            new CommandDefinition(
                "select organization_id from clinicians where id = @clinicianId",
                new { clinicianId },
                transaction,
                cancellationToken: ct
            )
        );

        var request = new PatientRecordRequest(
            ActorKind.Clinician,
            organizationId,
            clinicianId,
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
        var actor = new ClinicianActor(new ClinicianId(clinicianId));
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
