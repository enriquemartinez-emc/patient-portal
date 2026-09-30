namespace PatientPortal.Core.Domain;

public enum LabCategory
{
    Hematology,
    Biochemistry,
    Lipids,
    Endocrinology,
    Immunology,
    Microbiology,
    Urinalysis,
}

public readonly record struct LabTestName(string Value);

public readonly record struct LabValue(decimal Amount, string Unit);

public sealed record LabResult(
    LabResultId Id,
    PatientId Patient,
    LabCategory Category,
    LabTestName Test,
    LabValue Value,
    DateTimeOffset CollectedAt
)
{
    public bool Equals(LabResult? other) => other is not null && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
