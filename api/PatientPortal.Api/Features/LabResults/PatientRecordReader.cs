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

// The access check and the read share one transaction, so the treatment and consent rows the check
// relies on stay locked until the read commits. A read is recorded together with its results, so
// nothing is returned unrecorded, and a refusal is recorded too.
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

        var resource = new PatientRecordResource(
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
            resource,
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
            resource.VisibleCategories,
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
