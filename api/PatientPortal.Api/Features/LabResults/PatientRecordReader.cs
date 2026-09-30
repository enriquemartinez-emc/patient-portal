using Dapper;
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
        Guid clinicianId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    ) =>
        ReadAsync(
            connection,
            ActorKind.Clinician,
            new ClinicianActor(new ClinicianId(clinicianId)),
            clinicianId,
            patientId,
            time,
            ct
        );

    public static Task<PatientRecordRead> ReadAsResearcherAsync(
        NpgsqlConnection connection,
        Guid researcherId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    ) =>
        ReadAsync(
            connection,
            ActorKind.Researcher,
            new ResearcherActor(new ResearcherId(researcherId)),
            researcherId,
            patientId,
            time,
            ct
        );

    private static async Task<PatientRecordRead> ReadAsync(
        NpgsqlConnection connection,
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

        var decision = await PatientRecordAccess.DecideAsync(
            connection,
            transaction,
            new RecordAccessor(kind, actorId, organizationId),
            patientId,
            time.GetUtcNow(),
            ct
        );

        if (decision is not AccessGranted granted)
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
            granted.VisibleCategories,
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
