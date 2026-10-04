using HomeBase.Database.Enums;
using HomeBase.Features.MealPlan;

namespace HomeBase.Tests.MealPlan;

public class CookSelectionTests
{
    private static CookLine Line(decimal needed, decimal available, bool optional = false) =>
        new(1, "Mehl", BaseUnit.Gram, needed, available, optional);

    [Fact]
    public void For_PrefillsNeededQuantity()
    {
        Assert.Equal(300m, CookSelection.For(Line(300m, 0m)).Quantity);
    }

    [Theory]
    [InlineData(false, 0, true)]
    [InlineData(false, 500, true)]
    [InlineData(true, 500, true)]
    [InlineData(true, 0, false)]
    public void For_IncludesRequiredAndCoveredOptionalLines(
        bool optional,
        int available,
        bool expected
    )
    {
        Assert.Equal(expected, CookSelection.For(Line(100m, available, optional)).Include);
    }

    [Fact]
    public void ToBook_UsesAdjustedQuantityAsNeeded()
    {
        var selection = CookSelection.For(Line(300m, 1000m));
        selection.Quantity = 250m;

        var line = Assert.Single(CookSelection.ToBook([selection]));

        Assert.Equal(250m, line.Needed);
        Assert.Equal(1000m, line.Available);
    }

    [Fact]
    public void ToBook_SkipsExcludedAndZeroQuantityLines()
    {
        var excluded = CookSelection.For(Line(100m, 100m));
        excluded.Include = false;
        var zero = CookSelection.For(Line(100m, 100m));
        zero.Quantity = 0m;
        var kept = CookSelection.For(Line(50m, 100m));

        var lines = CookSelection.ToBook([excluded, zero, kept]);

        Assert.Equal([kept.Line], lines);
    }
}
