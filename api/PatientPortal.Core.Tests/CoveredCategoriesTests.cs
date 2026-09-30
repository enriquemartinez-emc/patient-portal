using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

public sealed class CoveredCategoriesTests
{
    private static ActiveConsent Active(LabCategory first, params LabCategory[] rest) =>
        ConsentTransitions.Grant(
            new ConsentGrantId(Guid.NewGuid()),
            TestData.Patient,
            TestData.Clinic,
            new ConsentScope(first, rest),
            new ConsentPurpose("Care"),
            new NeverExpires(),
            TestData.Now
        );

    [Fact]
    public void No_consents_cover_nothing()
    {
        Assert.Empty(ConsentRules.CoveredCategories([]));
    }

    [Fact]
    public void Several_active_consents_cover_the_union_of_their_scopes_once()
    {
        var covered = ConsentRules.CoveredCategories([
            Active(LabCategory.Hematology, LabCategory.Lipids),
            Active(LabCategory.Lipids, LabCategory.Urinalysis),
        ]);

        Assert.Equal(
            [LabCategory.Hematology, LabCategory.Lipids, LabCategory.Urinalysis],
            covered.OrderBy(category => category)
        );
    }

    [Fact]
    public void Revoked_and_expired_consents_cover_nothing()
    {
        var revoked = ConsentTransitions.Revoke(Active(LabCategory.Hematology), TestData.Now);
        var expired = new ExpiredConsent(
            new ConsentGrantId(Guid.NewGuid()),
            TestData.Patient,
            TestData.Clinic,
            new ConsentScope(LabCategory.Lipids, []),
            new ConsentPurpose("Care"),
            TestData.Now,
            TestData.Now.AddDays(1)
        );

        var covered = ConsentRules.CoveredCategories([
            revoked,
            expired,
            Active(LabCategory.Urinalysis),
        ]);

        Assert.Equal([LabCategory.Urinalysis], covered);
    }
}
