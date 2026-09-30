using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

public sealed class AuditEntriesTests
{
    private static LabResult Result(Guid id) =>
        new(
            new LabResultId(id),
            TestData.Patient,
            LabCategory.Hematology,
            new LabTestName("Hemoglobin"),
            new LabValue(13.5m, "g/dL"),
            TestData.Now
        );

    [Fact]
    public void A_lab_result_read_records_the_actor_the_patient_and_every_result_read()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var entryId = new AuditLogEntryId(Guid.NewGuid());
        var actor = new ClinicianActor(TestData.Clinician);

        var entry = AuditEntries.ForLabResultsRead(
            entryId,
            TestData.Now,
            actor,
            TestData.Patient,
            [Result(first), Result(second)]
        );

        Assert.Equal(entryId, entry.Id);
        Assert.Equal(TestData.Now, entry.OccurredAt);
        Assert.Equal(actor, entry.Actor);
        Assert.Equal(TestData.Patient, entry.Patient);
        var read = Assert.IsType<LabResultsRead>(entry.Action);
        Assert.Equal([new LabResultId(first), new LabResultId(second)], read.LabResults);
    }

    [Fact]
    public void A_read_that_returns_nothing_is_still_recorded()
    {
        var entry = AuditEntries.ForLabResultsRead(
            new AuditLogEntryId(Guid.NewGuid()),
            TestData.Now,
            new ClinicianActor(TestData.Clinician),
            TestData.Patient,
            []
        );

        var read = Assert.IsType<LabResultsRead>(entry.Action);
        Assert.Empty(read.LabResults);
    }

    [Fact]
    public void Granting_consent_is_recorded_as_an_action_of_the_patient()
    {
        var consent = TestData.ActiveConsent(new NeverExpires());

        var entry = AuditEntries.ForConsentGranted(
            new AuditLogEntryId(Guid.NewGuid()),
            TestData.Now,
            consent
        );

        Assert.Equal(new PatientActor(consent.Patient), entry.Actor);
        Assert.Equal(consent.Patient, entry.Patient);
        Assert.Equal(new ConsentGranted(consent.Id), entry.Action);
    }

    [Fact]
    public void Revoking_consent_is_recorded_as_an_action_of_the_patient()
    {
        var revoked = ConsentTransitions.Revoke(
            TestData.ActiveConsent(new NeverExpires()),
            TestData.Now.AddDays(1)
        );

        var entry = AuditEntries.ForConsentRevoked(
            new AuditLogEntryId(Guid.NewGuid()),
            TestData.Now.AddDays(1),
            revoked
        );

        Assert.Equal(new PatientActor(revoked.Patient), entry.Actor);
        Assert.Equal(new ConsentRevoked(revoked.Id), entry.Action);
    }

    [Fact]
    public void A_refused_read_records_who_asked_and_whose_records_they_asked_for()
    {
        var actor = new ResearcherActor(new ResearcherId(Guid.NewGuid()));

        var entry = AuditEntries.ForAccessDenied(
            new AuditLogEntryId(Guid.NewGuid()),
            TestData.Now,
            actor,
            TestData.Patient
        );

        Assert.Equal(actor, entry.Actor);
        Assert.Equal(TestData.Patient, entry.Patient);
        Assert.Equal(new AccessDenied(), entry.Action);
    }
}
