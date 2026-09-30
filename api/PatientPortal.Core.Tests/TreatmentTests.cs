using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

public sealed class TreatmentTests
{
    private static ActiveTreatment Started() =>
        TreatmentTransitions.Start(
            new TreatmentRelationshipId(Guid.NewGuid()),
            TestData.Patient,
            TestData.Clinician,
            TestData.Now
        );

    [Fact]
    public void Start_creates_an_active_treatment()
    {
        var treatment = Started();

        Assert.Equal(TestData.Patient, treatment.Patient);
        Assert.Equal(TestData.Clinician, treatment.Clinician);
        Assert.Equal(TestData.Now, treatment.StartedAt);
    }

    [Fact]
    public void End_keeps_the_relationship_and_records_when_it_ended()
    {
        var active = Started();
        var later = TestData.Now.AddDays(90);

        var ended = TreatmentTransitions.End(active, later);

        Assert.Equal(active.Id, ended.Id);
        Assert.Equal(active.Patient, ended.Patient);
        Assert.Equal(active.Clinician, ended.Clinician);
        Assert.Equal(active.StartedAt, ended.StartedAt);
        Assert.Equal(later, ended.EndedAt);
    }
}
