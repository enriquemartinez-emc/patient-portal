namespace PatientPortal.Core.Domain;

public abstract record AuditActor
{
    private protected AuditActor() { }
}

public sealed record PatientActor(PatientId Patient) : AuditActor;

public sealed record ClinicianActor(ClinicianId Clinician) : AuditActor;

public sealed record ResearcherActor(ResearcherId Researcher) : AuditActor;

public abstract record AuditAction
{
    private protected AuditAction() { }
}

public sealed record LabResultsRead(IReadOnlyList<LabResultId> LabResults) : AuditAction;

public sealed record ConsentGranted(ConsentGrantId Consent) : AuditAction;

public sealed record ConsentRevoked(ConsentGrantId Consent) : AuditAction;

public sealed record AccessDenied : AuditAction;

public sealed record AuditLogEntry(
    AuditLogEntryId Id,
    DateTimeOffset OccurredAt,
    AuditActor Actor,
    PatientId Patient,
    AuditAction Action
);

public static class AuditEntries
{
    public static AuditLogEntry ForLabResultsRead(
        AuditLogEntryId id,
        DateTimeOffset now,
        AuditActor actor,
        PatientId patient,
        IReadOnlyList<LabResult> results
    ) => new(id, now, actor, patient, new LabResultsRead([.. results.Select(r => r.Id)]));

    public static AuditLogEntry ForConsentGranted(
        AuditLogEntryId id,
        DateTimeOffset now,
        ActiveConsent consent
    ) =>
        new(
            id,
            now,
            new PatientActor(consent.Patient),
            consent.Patient,
            new ConsentGranted(consent.Id)
        );

    public static AuditLogEntry ForConsentRevoked(
        AuditLogEntryId id,
        DateTimeOffset now,
        RevokedConsent consent
    ) =>
        new(
            id,
            now,
            new PatientActor(consent.Patient),
            consent.Patient,
            new ConsentRevoked(consent.Id)
        );

    public static AuditLogEntry ForAccessDenied(
        AuditLogEntryId id,
        DateTimeOffset now,
        AuditActor actor,
        PatientId patient
    ) => new(id, now, actor, patient, new AccessDenied());
}
