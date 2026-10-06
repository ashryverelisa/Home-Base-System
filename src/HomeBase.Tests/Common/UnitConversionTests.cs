using HomeBase.Database.Enums;
using HomeBase.Features.Common;

namespace HomeBase.Tests.Common;

public class UnitConversionTests
{
    [Theory]
    [InlineData("g", BaseUnit.Gram, 1)]
    [InlineData(" KG ", BaseUnit.Gram, 1000)]
    [InlineData("ml", BaseUnit.Milliliter, 1)]
    [InlineData("cl", BaseUnit.Milliliter, 10)]
    [InlineData("Liter", BaseUnit.Milliliter, 1000)]
    public void Metric_KnownUnits(string unit, BaseUnit expectedUnit, double factor)
    {
        Assert.Equal((expectedUnit, (decimal)factor), UnitConversion.Metric(unit));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("stk")]
    [InlineData("EL")]
    public void Metric_OtherUnits_AreNull(string? unit)
    {
        Assert.Null(UnitConversion.Metric(unit));
    }

    [Fact]
    public void ToBase_OnlyConvertsIntoTheProductsUnit()
    {
        Assert.Equal(1500m, UnitConversion.ToBase(1.5m, "kg", BaseUnit.Gram));
        Assert.Null(UnitConversion.ToBase(1.5m, "kg", BaseUnit.Milliliter));
        Assert.Null(UnitConversion.ToBase(2m, "stk", BaseUnit.Piece));
    }
}
