using Dapper;
using Npgsql;
using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Infrastructure.Auditing;

// Every audited action inserts through here, on the caller's transaction, so the audit row
// commits or rolls back together with the change or read it describes.
public static class AuditLogWriter
{
    public static async Task InsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        AuditLogEntry entry,
        CancellationToken ct
    )
    {
        var (actorKind, actorId) = entry.Actor switch
        {
            PatientActor actor => ("patient", actor.Patient.Value),
            ClinicianActor actor => ("clinician", actor.Clinician.Value),
            ResearcherActor actor => ("researcher", actor.Researcher.Value),
            _ => throw new InvalidOperationException($"Unsupported audit actor '{entry.Actor}'."),
        };

        var (action, labResultIds, consentGrantId) = entry.Action switch
        {
            LabResultsRead read => (
                "lab_results_read",
                read.LabResults.Select(id => id.Value).ToArray(),
                (Guid?)null
            ),
            ConsentGranted granted => (
                "consent_granted",
                Array.Empty<Guid>(),
                (Guid?)granted.Consent.Value
            ),
            ConsentRevoked revoked => (
                "consent_revoked",
                Array.Empty<Guid>(),
                (Guid?)revoked.Consent.Value
            ),
            AccessDenied => ("access_denied", Array.Empty<Guid>(), (Guid?)null),
            _ => throw new InvalidOperationException($"Unsupported audit action '{entry.Action}'."),
        };

        // Inserting through a SELECT lets a refusal for a patient that does not exist record nothing,
        // instead of failing on the foreign key and turning a 403 into a 500.
        const string sql = """
            insert into audit_log
                (id, occurred_at, actor_kind, actor_id, patient_id, action, lab_result_ids, consent_grant_id)
            select
                @Id, @OccurredAt, @ActorKind, @ActorId, @PatientId, @Action, @LabResultIds, @ConsentGrantId
            where exists (select 1 from patients where id = @PatientId)
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = entry.Id.Value,
                    entry.OccurredAt,
                    ActorKind = actorKind,
                    ActorId = actorId,
                    PatientId = entry.Patient.Value,
                    Action = action,
                    LabResultIds = labResultIds,
                    ConsentGrantId = consentGrantId,
                },
                transaction,
                cancellationToken: ct
            )
        );
    }
}
