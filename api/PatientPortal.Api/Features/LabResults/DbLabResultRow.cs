using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.LabResults;

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

    public LabResult ToLabResult(PatientId patient) =>
        new(
            new LabResultId(Id),
            patient,
            LabCategoryNames.TryParse(Category, out var category)
                ? category
                : throw new InvalidOperationException($"Unsupported lab category '{Category}'."),
            new LabTestName(TestName),
            new LabValue(Value, Unit),
            CollectedAt
        );
}
