namespace PatientPortal.Core.Domain;

public readonly record struct PatientId(Guid Value);

public readonly record struct ClinicianId(Guid Value);

public readonly record struct ResearcherId(Guid Value);

public readonly record struct OrganizationId(Guid Value);

public readonly record struct LabResultId(Guid Value);

public readonly record struct ConsentGrantId(Guid Value);

public readonly record struct TreatmentRelationshipId(Guid Value);

public readonly record struct AuditLogEntryId(Guid Value);
