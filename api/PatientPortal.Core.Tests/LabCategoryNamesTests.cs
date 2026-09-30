using PatientPortal.Core.Domain;

namespace PatientPortal.Core.Tests;

public sealed class LabCategoryNamesTests
{
    [Fact]
    public void Every_category_round_trips_through_its_name()
    {
        foreach (var category in LabCategoryNames.All)
        {
            Assert.True(
                LabCategoryNames.TryParse(LabCategoryNames.ToName(category), out var parsed)
            );
            Assert.Equal(category, parsed);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Hematology")]
    [InlineData("not_a_category")]
    [InlineData(null)]
    public void Unknown_names_are_not_parsed(string? name)
    {
        Assert.False(LabCategoryNames.TryParse(name, out _));
    }
}
