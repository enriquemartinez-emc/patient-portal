namespace PatientPortal.Api.Features.Clinicians;

internal sealed record DbPatientRow(
    Guid Id,
    string FullName,
    DateOnly DateOfBirth,
    bool HasTreatment,
    bool HasConsent
)
{
    public PatientSummaryResponse ToResponse() =>
        new(
            Id,
            FullName,
            DateOfBirth,
            [
                .. new[]
                {
                    HasTreatment ? "treatment" : null,
                    HasConsent ? "consent" : null,
                }.OfType<string>(),
            ]
        );
}
