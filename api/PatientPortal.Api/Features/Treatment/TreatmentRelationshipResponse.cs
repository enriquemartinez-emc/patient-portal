using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Treatment;

public sealed record TreatmentRelationshipResponse(
    Guid Id,
    Guid PatientId,
    Guid ClinicianId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Status
)
{
    public static TreatmentRelationshipResponse From(TreatmentRelationship treatment) =>
        new(
            treatment.Id.Value,
            treatment.Patient.Value,
            treatment.Clinician.Value,
            treatment.StartedAt,
            treatment is EndedTreatment ended ? ended.EndedAt : null,
            treatment switch
            {
                ActiveTreatment => "active",
                EndedTreatment => "ended",
                _ => throw new InvalidOperationException(
                    $"Unsupported treatment state '{treatment}'."
                ),
            }
        );
}
