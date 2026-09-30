using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using PatientPortal.Api.Infrastructure.Auditing;
using PatientPortal.Api.Infrastructure.Auth;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.LabResults;

internal abstract record PatientRecordRead
{
    private protected PatientRecordRead() { }
}

internal sealed record RecordReadGranted(IReadOnlyList<DbLabResultRow> Results) : PatientRecordRead;

internal sealed record RecordReadRefused : PatientRecordRead;

// The audited read of one patient's lab results by a clinician or a researcher, shared by both
// endpoints. It answers in domain terms; turning that into an HTTP response is the endpoint's job.
//
// The access check and the read share one transaction, so the treatment and consent rows the check
// relies on stay locked until the read commits and a revoke cannot slip in between the two. A read
// is recorded together with its results, so nothing is returned unrecorded, and a refusal is
// recorded in the patient's audit trail too.
internal static class PatientRecordReader
{
    public static Task<PatientRecordRead> ReadAsClinicianAsync(
        NpgsqlConnection connection,
        ClaimsPrincipal user,
        IAuthorizationService authorization,
        Guid clinicianId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    ) =>
        ReadAsync(
            connection,
            user,
            authorization,
            ActorKind.Clinician,
            new ClinicianActor(new ClinicianId(clinicianId)),
            clinicianId,
            patientId,
            time,
            ct
        );

    public static Task<PatientRecordRead> ReadAsResearcherAsync(
        NpgsqlConnection connection,
        ClaimsPrincipal user,
        IAuthorizationService authorization,
        Guid researcherId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    ) =>
        ReadAsync(
            connection,
            user,
            authorization,
            ActorKind.Researcher,
            new ResearcherActor(new ResearcherId(researcherId)),
            researcherId,
            patientId,
            time,
            ct
        );

    private static async Task<PatientRecordRead> ReadAsync(
        NpgsqlConnection connection,
        ClaimsPrincipal user,
        IAuthorizationService authorization,
        ActorKind kind,
        AuditActor actor,
        Guid actorId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    )
    {
        var patient = new PatientId(patientId);

        await using var transaction = await connection.BeginTransactionAsync(ct);

        var organizationId = await OrganizationOfAsync(connection, transaction, kind, actorId, ct);

        var request = new PatientRecordRequest(
            kind,
            organizationId,
            actorId,
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

        if (!decision.Succeeded)
        {
            // Ends the read, releasing its locks, then records the refusal on its own so that it is
            // kept even though the read is not.
            await transaction.RollbackAsync(ct);
            await AuditLogWriter.InsertAsync(
                connection,
                transaction: null,
                AuditEntries.ForAccessDenied(
                    new AuditLogEntryId(Guid.CreateVersion7()),
                    time.GetUtcNow(),
                    actor,
                    patient
                ),
                ct
            );
            return new RecordReadRefused();
        }

        var rows = await LabResultsQuery.InCategoriesAsync(
            connection,
            transaction,
            patientId,
            request.VisibleCategories,
            ct
        );

        var audit = AuditEntries.ForLabResultsRead(
            new AuditLogEntryId(Guid.CreateVersion7()),
            time.GetUtcNow(),
            actor,
            patient,
            [.. rows.Select(row => row.ToLabResult(patient))]
        );
        await AuditLogWriter.InsertAsync(connection, transaction, audit, ct);

        await transaction.CommitAsync(ct);
        return new RecordReadGranted(rows);
    }

    // The caller has already been checked to be this clinician or researcher, so the record exists.
    private static Task<Guid> OrganizationOfAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ActorKind kind,
        Guid actorId,
        CancellationToken ct
    )
    {
        var sql = kind switch
        {
            ActorKind.Clinician => "select organization_id from clinicians where id = @actorId",
            ActorKind.Researcher => "select organization_id from researchers where id = @actorId",
            _ => throw new InvalidOperationException($"Unsupported actor '{kind}'."),
        };

        return connection.QuerySingleAsync<Guid>(
            new CommandDefinition(sql, new { actorId }, transaction, cancellationToken: ct)
        );
    }
}
