namespace PatientPortal.Api.Features.LabResults;

// Shared by the two lab result reads, which select the same columns.
internal sealed record DbLabResultRow(
    Guid Id,
    string Category,
    string TestName,
    decimal Value,
    string Unit,
    DateTimeOffset CollectedAt
)
{
    public LabResultResponse ToResponse() => new(Id, Category, TestName, Value, Unit, CollectedAt);
}
