using Dapper;
using Npgsql;
using PatientPortal.Api.Features.Consents;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Infrastructure.Auth;

// Who is asking to read a patient's record: a clinician or researcher, and the organization they act for.
public sealed record RecordAccessor(ActorKind Kind, Guid ActorId, Guid OrganizationId);

public abstract record AccessDecision
{
    private protected AccessDecision() { }
}

public sealed record AccessGranted(IReadOnlyList<LabCategory> VisibleCategories) : AccessDecision;

public sealed record AccessDenied : AccessDecision;

// Decides which of a patient's lab result categories an accessor may see: all of them for a
// clinician with an active treatment relationship, otherwise the categories the patient's consents
// grant the accessor's organization.
//
// It runs in the caller's open transaction: the rows it relies on stay locked until the read
// commits, so a revoke cannot slip in between the decision and the read.
public static class PatientRecordAccess
{
    public static async Task<AccessDecision> DecideAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        RecordAccessor accessor,
        Guid patientId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var categories =
            accessor.Kind == ActorKind.Clinician
            && await IsTreatingAsync(connection, transaction, accessor.ActorId, patientId, ct)
                ? LabCategoryNames.All
                : await ConsentCoverage.CoveredCategoriesAsync(
                    connection,
                    transaction,
                    patientId,
                    accessor.OrganizationId,
                    now,
                    ct
                );

        return categories.Count > 0 ? new AccessGranted(categories) : new AccessDenied();
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
