namespace PatientPortal.Core.Domain;

public readonly record struct PersonName(string Value);

public readonly record struct OrganizationName(string Value);

public enum OrganizationKind
{
    Clinic,
    ResearchInstitution,
}

public sealed record Organization(OrganizationId Id, OrganizationName Name, OrganizationKind Kind)
{
    public bool Equals(Organization? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record Patient(PatientId Id, PersonName Name, DateOnly DateOfBirth)
{
    public bool Equals(Patient? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record Clinician(ClinicianId Id, OrganizationId Organization, PersonName Name)
{
    public bool Equals(Clinician? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record Researcher(ResearcherId Id, OrganizationId Organization, PersonName Name)
{
    public bool Equals(Researcher? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
