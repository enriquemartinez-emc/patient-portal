using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

public sealed class ConsentTests
{
    [Fact]
    public void Grant_creates_an_active_consent_stamped_with_now()
    {
        var consent = TestData.ActiveConsent(new NeverExpires());

        Assert.Equal(TestData.Now, consent.GrantedAt);
        Assert.Equal(TestData.Patient, consent.Patient);
        Assert.Equal(TestData.Clinic, consent.Grantee);
        Assert.Equal(new NeverExpires(), consent.Expiry);
    }

    [Fact]
    public void Revoke_carries_the_grant_forward_and_records_when_it_was_revoked()
    {
        var active = TestData.ActiveConsent(new NeverExpires());
        var later = TestData.Now.AddDays(3);

        var revoked = ConsentTransitions.Revoke(active, later);

        Assert.Equal(active.Id, revoked.Id);
        Assert.Equal(active.Patient, revoked.Patient);
        Assert.Equal(active.Grantee, revoked.Grantee);
        Assert.Equal(active.Scope, revoked.Scope);
        Assert.Equal(active.Purpose, revoked.Purpose);
        Assert.Equal(active.GrantedAt, revoked.GrantedAt);
        Assert.Equal(later, revoked.RevokedAt);
    }

    [Fact]
    public void A_consent_that_never_expires_stays_active_forever()
    {
        var active = TestData.ActiveConsent(new NeverExpires());

        var result = ConsentRules.ApplyExpiry(active, TestData.Now.AddYears(100));

        Assert.Same(active, result);
    }

    [Fact]
    public void A_consent_is_still_active_just_before_its_expiry()
    {
        var expiry = TestData.Now.AddDays(10);
        var active = TestData.ActiveConsent(new ExpiresAt(expiry));

        var result = ConsentRules.ApplyExpiry(active, expiry.AddTicks(-1));

        Assert.Same(active, result);
    }

    [Fact]
    public void A_consent_expires_exactly_at_its_expiry_instant()
    {
        var expiry = TestData.Now.AddDays(10);
        var active = TestData.ActiveConsent(new ExpiresAt(expiry));

        var result = ConsentRules.ApplyExpiry(active, expiry);

        var expired = Assert.IsType<ExpiredConsent>(result);
        Assert.Equal(active.Id, expired.Id);
        Assert.Equal(expiry, expired.ExpiredAt);
    }

    [Fact]
    public void A_consent_stays_expired_after_its_expiry_instant()
    {
        var expiry = TestData.Now.AddDays(10);
        var active = TestData.ActiveConsent(new ExpiresAt(expiry));

        var result = ConsentRules.ApplyExpiry(active, expiry.AddDays(30));

        var expired = Assert.IsType<ExpiredConsent>(result);
        Assert.Equal(expiry, expired.ExpiredAt);
    }
}
