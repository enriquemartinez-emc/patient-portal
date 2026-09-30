using Dapper;
using Npgsql;
using PatientPortal.Api.Common;
using PatientPortal.Api.Features.Consents;
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
        Guid clinicianId,
        Guid organizationId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    ) =>
        ReadAsync(
            connection,
            new ClinicianActor(new ClinicianId(clinicianId)),
            organizationId,
            patientId,
            time,
            ct
        );

    public static Task<PatientRecordRead> ReadAsResearcherAsync(
        NpgsqlConnection connection,
        Guid researcherId,
        Guid organizationId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    ) =>
        ReadAsync(
            connection,
            new ResearcherActor(new ResearcherId(researcherId)),
            organizationId,
            patientId,
            time,
            ct
        );

    private static async Task<PatientRecordRead> ReadAsync(
        NpgsqlConnection connection,
        AuditActor actor,
        Guid organizationId,
        Guid patientId,
        TimeProvider time,
        CancellationToken ct
    )
    {
        var now = time.GetUtcNow();
        var patient = new PatientId(patientId);

        await using var transaction = await connection.BeginTransactionAsync(ct);

        var basis = await LoadAccessBasisAsync(
            connection,
            transaction,
            actor,
            organizationId,
            patientId,
            now,
            ct
        );

        if (RecordAccessRules.Decide(basis) is not AccessGranted granted)
        {
            // Ends the read, releasing its locks, then records the refusal on its own so that it is
            // kept even though the read is not.
            await transaction.RollbackAsync(ct);
            await AuditLogWriter.InsertAsync(
                connection,
                transaction: null,
                AuditEntries.ForAccessDenied(
                    new AuditLogEntryId(Guid.CreateVersion7()),
                    now,
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
            granted.Categories,
            ct
        );

        var audit = AuditEntries.ForLabResultsRead(
            new AuditLogEntryId(Guid.CreateVersion7()),
            now,
            actor,
            patient,
            [.. rows.Select(row => row.ToLabResult(patient))]
        );
        await AuditLogWriter.InsertAsync(connection, transaction, audit, ct);

        await transaction.CommitAsync(ct);
        return new RecordReadGranted(rows);
    }

    // A treating clinician needs no consent, so the consents are only loaded (and locked) for
    // everyone else.
    private static async Task<RecordAccessBasis> LoadAccessBasisAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        AuditActor actor,
        Guid organizationId,
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        if (
            actor is ClinicianActor clinician
            && await IsTreatingAsync(
                connection,
                transaction,
                clinician.Clinician.Value,
                patientId,
                ct
            )
        )
        {
            return new TreatingClinician();
        }

        return new ConsentsToOrganization(
            await ConsentCoverage.LoadGrantsAsync(
                connection,
                transaction,
                patientId,
                organizationId,
                now,
                ct
            )
        );
    }

    // FOR SHARE keeps a concurrent "end treatment" from committing mid-read.
    private static async Task<bool> IsTreatingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid clinicianId,
        Guid patientId,
        CancellationToken ct
    ) =>
        await connection.QuerySingleOrDefaultAsync<Guid?>(
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
        )
            is not null;
}
