namespace PatientPortal.Core.Domain;

public readonly record struct ConsentPurpose(string Value);

// A scope always names at least one lab category, so it is a required first element plus the rest.
public sealed record ConsentScope(LabCategory First, IReadOnlyList<LabCategory> Rest);

public abstract record ConsentExpiry
{
    private protected ConsentExpiry() { }
}

public sealed record NeverExpires : ConsentExpiry;

public sealed record ExpiresAt(DateTimeOffset Instant) : ConsentExpiry;

public abstract record ConsentGrant
{
    private protected ConsentGrant(
        ConsentGrantId id,
        PatientId patient,
        OrganizationId grantee,
        ConsentScope scope,
        ConsentPurpose purpose,
        DateTimeOffset grantedAt
    )
    {
        Id = id;
        Patient = patient;
        Grantee = grantee;
        Scope = scope;
        Purpose = purpose;
        GrantedAt = grantedAt;
    }

    public ConsentGrantId Id { get; }
    public PatientId Patient { get; }
    public OrganizationId Grantee { get; }
    public ConsentScope Scope { get; }
    public ConsentPurpose Purpose { get; }
    public DateTimeOffset GrantedAt { get; }
}

public sealed record ActiveConsent(
    ConsentGrantId Id,
    PatientId Patient,
    OrganizationId Grantee,
    ConsentScope Scope,
    ConsentPurpose Purpose,
    DateTimeOffset GrantedAt,
    ConsentExpiry Expiry
) : ConsentGrant(Id, Patient, Grantee, Scope, Purpose, GrantedAt);

public sealed record RevokedConsent(
    ConsentGrantId Id,
    PatientId Patient,
    OrganizationId Grantee,
    ConsentScope Scope,
    ConsentPurpose Purpose,
    DateTimeOffset GrantedAt,
    DateTimeOffset RevokedAt
) : ConsentGrant(Id, Patient, Grantee, Scope, Purpose, GrantedAt);

public sealed record ExpiredConsent(
    ConsentGrantId Id,
    PatientId Patient,
    OrganizationId Grantee,
    ConsentScope Scope,
    ConsentPurpose Purpose,
    DateTimeOffset GrantedAt,
    DateTimeOffset ExpiredAt
) : ConsentGrant(Id, Patient, Grantee, Scope, Purpose, GrantedAt);

public static class ConsentTransitions
{
    public static ActiveConsent Grant(
        ConsentGrantId id,
        PatientId patient,
        OrganizationId grantee,
        ConsentScope scope,
        ConsentPurpose purpose,
        ConsentExpiry expiry,
        DateTimeOffset now
    ) => new(id, patient, grantee, scope, purpose, now, expiry);

    public static RevokedConsent Revoke(ActiveConsent consent, DateTimeOffset now) =>
        new(
            consent.Id,
            consent.Patient,
            consent.Grantee,
            consent.Scope,
            consent.Purpose,
            consent.GrantedAt,
            now
        );
}

public static class ConsentRules
{
    // Expiry is derived from the clock rather than stored, so an active consent is
    // re-classified whenever it is loaded. A consent expires at its expiry instant, not after it.
    public static ConsentGrant ApplyExpiry(ActiveConsent consent, DateTimeOffset now) =>
        consent.Expiry switch
        {
            ExpiresAt expiresAt when now >= expiresAt.Instant => new ExpiredConsent(
                consent.Id,
                consent.Patient,
                consent.Grantee,
                consent.Scope,
                consent.Purpose,
                consent.GrantedAt,
                expiresAt.Instant
            ),
            _ => consent,
        };

    // The lab categories a set of consents makes visible: the union of the scopes of those in
    // effect. Revoked and expired consents contribute nothing.
    public static IReadOnlyList<LabCategory> CoveredCategories(
        IReadOnlyList<ConsentGrant> grants
    ) =>
        [
            .. grants
                .OfType<ActiveConsent>()
                .SelectMany(grant => grant.Scope.Rest.Prepend(grant.Scope.First))
                .Distinct(),
        ];
}
