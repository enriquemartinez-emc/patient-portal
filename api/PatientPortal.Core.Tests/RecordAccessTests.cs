using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

public sealed class RecordAccessTests
{
    [Fact]
    public void A_treating_clinician_sees_every_category()
    {
        var decision = RecordAccessRules.Decide(new TreatingClinician());

        var granted = Assert.IsType<AccessGranted>(decision);
        Assert.Equal(LabCategoryNames.All, granted.Categories);
    }

    [Fact]
    public void Consent_grants_access_to_the_categories_it_covers()
    {
        var consent = TestData.ActiveConsent(new NeverExpires());

        var decision = RecordAccessRules.Decide(new ConsentsToOrganization([consent]));

        var granted = Assert.IsType<AccessGranted>(decision);
        Assert.Equal(
            [LabCategory.Hematology, LabCategory.Lipids],
            granted.Categories.OrderBy(category => category)
        );
    }

    [Fact]
    public void Without_a_consent_in_effect_access_is_refused()
    {
        var revoked = ConsentTransitions.Revoke(
            TestData.ActiveConsent(new NeverExpires()),
            TestData.Now
        );

        Assert.IsType<AccessRefused>(RecordAccessRules.Decide(new ConsentsToOrganization([])));
        Assert.IsType<AccessRefused>(
            RecordAccessRules.Decide(new ConsentsToOrganization([revoked]))
        );
    }
}
