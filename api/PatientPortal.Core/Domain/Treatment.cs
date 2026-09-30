namespace PatientPortal.Core.Domain;

public abstract record TreatmentRelationship
{
    private protected TreatmentRelationship(
        TreatmentRelationshipId id,
        PatientId patient,
        ClinicianId clinician,
        DateTimeOffset startedAt
    )
    {
        Id = id;
        Patient = patient;
        Clinician = clinician;
        StartedAt = startedAt;
    }

    public TreatmentRelationshipId Id { get; }
    public PatientId Patient { get; }
    public ClinicianId Clinician { get; }
    public DateTimeOffset StartedAt { get; }
}

public sealed record ActiveTreatment(
    TreatmentRelationshipId Id,
    PatientId Patient,
    ClinicianId Clinician,
    DateTimeOffset StartedAt
) : TreatmentRelationship(Id, Patient, Clinician, StartedAt);

public sealed record EndedTreatment(
    TreatmentRelationshipId Id,
    PatientId Patient,
    ClinicianId Clinician,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt
) : TreatmentRelationship(Id, Patient, Clinician, StartedAt);

public static class TreatmentTransitions
{
    public static ActiveTreatment Start(
        TreatmentRelationshipId id,
        PatientId patient,
        ClinicianId clinician,
        DateTimeOffset now
    ) => new(id, patient, clinician, now);

    public static EndedTreatment End(ActiveTreatment treatment, DateTimeOffset now) =>
        new(treatment.Id, treatment.Patient, treatment.Clinician, treatment.StartedAt, now);
}
