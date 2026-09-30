using PatientPortal.Core.Domain;

namespace PatientPortal.Api.Features.Consents;

public sealed record ConsentResponse(
    Guid Id,
    Guid GranteeOrganizationId,
    string GranteeName,
    IReadOnlyList<string> Categories,
    string Purpose,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string Status
)
{
    public static ConsentResponse From(
        ConsentGrant grant,
        string granteeName,
        DateTimeOffset? expiresAt
    ) =>
        new(
            grant.Id.Value,
            grant.Grantee.Value,
            granteeName,
            [
                LabCategoryNames.ToName(grant.Scope.First),
                .. grant.Scope.Rest.Select(LabCategoryNames.ToName),
            ],
            grant.Purpose.Value,
            grant.GrantedAt,
            expiresAt,
            grant is RevokedConsent revoked ? revoked.RevokedAt : null,
            grant switch
            {
                ActiveConsent => "active",
                RevokedConsent => "revoked",
                ExpiredConsent => "expired",
                _ => throw new InvalidOperationException($"Unsupported consent state '{grant}'."),
            }
        );
}
