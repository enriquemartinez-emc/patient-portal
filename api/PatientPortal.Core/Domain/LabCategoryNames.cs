namespace PatientPortal.Core.Domain;

// The wire and storage names of the lab categories. Kept explicit so renaming an enum member
// can never silently change the API contract or what is stored.
public static class LabCategoryNames
{
    public static readonly IReadOnlyList<LabCategory> All = Enum.GetValues<LabCategory>();

    public static string ToName(LabCategory category) =>
        category switch
        {
            LabCategory.Hematology => "hematology",
            LabCategory.Biochemistry => "biochemistry",
            LabCategory.Lipids => "lipids",
            LabCategory.Endocrinology => "endocrinology",
            LabCategory.Immunology => "immunology",
            LabCategory.Microbiology => "microbiology",
            LabCategory.Urinalysis => "urinalysis",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

    public static bool TryParse(string? name, out LabCategory category)
    {
        (var known, category) = name switch
        {
            "hematology" => (true, LabCategory.Hematology),
            "biochemistry" => (true, LabCategory.Biochemistry),
            "lipids" => (true, LabCategory.Lipids),
            "endocrinology" => (true, LabCategory.Endocrinology),
            "immunology" => (true, LabCategory.Immunology),
            "microbiology" => (true, LabCategory.Microbiology),
            "urinalysis" => (true, LabCategory.Urinalysis),
            _ => (false, default(LabCategory)),
        };
        return known;
    }
}
