using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Consents;

internal sealed record DbConsentRow(
    Guid Id,
    Guid PatientId,
    Guid GranteeOrganizationId,
    string GranteeName,
    string[] Categories,
    string Purpose,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiryInstant,
    DateTimeOffset? RevokedInstant
)
{
    public const string SelectSql = """
        select c.id as Id, c.patient_id as PatientId,
               c.grantee_organization_id as GranteeOrganizationId, o.name as GranteeName,
               c.categories as Categories, c.purpose as Purpose, c.granted_at as GrantedAt,
               c.expires_at as ExpiryInstant, c.revoked_at as RevokedInstant
        from consent_grants c
        join organizations o on o.id = c.grantee_organization_id
        """;

    public ConsentGrant ToConsent(DateTimeOffset now)
    {
        var scope = ToScope();

        if (RevokedInstant is { } revokedAt)
        {
            return new RevokedConsent(
                new ConsentGrantId(Id),
                new PatientId(PatientId),
                new OrganizationId(GranteeOrganizationId),
                scope,
                new ConsentPurpose(Purpose),
                GrantedAt,
                revokedAt
            );
        }

        ConsentExpiry expiry = ExpiryInstant is { } expiresAt
            ? new ExpiresAt(expiresAt)
            : new NeverExpires();

        return ConsentRules.ApplyExpiry(
            new ActiveConsent(
                new ConsentGrantId(Id),
                new PatientId(PatientId),
                new OrganizationId(GranteeOrganizationId),
                scope,
                new ConsentPurpose(Purpose),
                GrantedAt,
                expiry
            ),
            now
        );
    }

    public ConsentResponse ToResponse(DateTimeOffset now) =>
        ConsentResponse.From(ToConsent(now), GranteeName, ExpiryInstant);

    private ConsentScope ToScope()
    {
        var categories = Categories
            .Select(name =>
                LabCategoryNames.TryParse(name, out var category)
                    ? category
                    : throw new InvalidOperationException($"Unsupported lab category '{name}'.")
            )
            .ToArray();

        return new ConsentScope(categories[0], categories[1..]);
    }
}
