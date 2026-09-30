namespace PatientPortal.Core.Domain;

// Why someone may be allowed to read a patient's lab results. A clinician who treats the patient
// reads everything; anyone else reads only what the patient's consents to their organization cover.
public abstract record RecordAccessBasis
{
    private protected RecordAccessBasis() { }
}

public sealed record TreatingClinician : RecordAccessBasis;

public sealed record ConsentsToOrganization(IReadOnlyList<ConsentGrant> Grants) : RecordAccessBasis;

public abstract record RecordAccessDecision
{
    private protected RecordAccessDecision() { }
}

public sealed record AccessGranted(IReadOnlyList<LabCategory> Categories) : RecordAccessDecision;

public sealed record AccessRefused : RecordAccessDecision;

public static class RecordAccessRules
{
    // No treatment and no consent in effect means no access: a grant never has an empty scope, so
    // "covers nothing" can only mean that no consent is in effect.
    public static RecordAccessDecision Decide(RecordAccessBasis basis)
    {
        var categories = basis switch
        {
            TreatingClinician => LabCategoryNames.All,
            ConsentsToOrganization consents => ConsentRules.CoveredCategories(consents.Grants),
            _ => throw new InvalidOperationException($"Unsupported access basis '{basis}'."),
        };

        return categories.Count > 0 ? new AccessGranted(categories) : new AccessRefused();
    }
}
