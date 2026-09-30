using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Treatment;

internal sealed record DbTreatmentRow(
    Guid Id,
    Guid PatientId,
    Guid ClinicianId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedInstant
)
{
    public const string SelectSql = """
        select id as Id, patient_id as PatientId, clinician_id as ClinicianId,
               started_at as StartedAt, ended_at as EndedInstant
        from treatment_relationships
        """;

    public TreatmentRelationship ToTreatment() =>
        EndedInstant is { } endedAt
            ? new EndedTreatment(
                new TreatmentRelationshipId(Id),
                new PatientId(PatientId),
                new ClinicianId(ClinicianId),
                StartedAt,
                endedAt
            )
            : new ActiveTreatment(
                new TreatmentRelationshipId(Id),
                new PatientId(PatientId),
                new ClinicianId(ClinicianId),
                StartedAt
            );
}
