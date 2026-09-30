using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    public static readonly PatientId Patient = new(
        Guid.Parse("b0000000-0000-0000-0000-000000000001")
    );
    public static readonly ClinicianId Clinician = new(
        Guid.Parse("c0000000-0000-0000-0000-000000000001")
    );
    public static readonly OrganizationId Clinic = new(
        Guid.Parse("a0000000-0000-0000-0000-000000000002")
    );

    public static readonly ConsentScope Scope = new(LabCategory.Hematology, [LabCategory.Lipids]);

    public static ActiveConsent ActiveConsent(ConsentExpiry expiry) =>
        ConsentTransitions.Grant(
            new ConsentGrantId(Guid.NewGuid()),
            Patient,
            Clinic,
            Scope,
            new ConsentPurpose("Continuity of care"),
            expiry,
            Now
        );
}
