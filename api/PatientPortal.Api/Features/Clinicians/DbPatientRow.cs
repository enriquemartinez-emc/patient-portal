namespace PatientPortal.Api.Features.Clinicians;

// Shared by the clinician's patient list and patient summary.
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
