using HomeBase.Database.Enums;
using HomeBase.Features.Common;

namespace HomeBase.Tests.Common;

public class UnitsTests
{
    [Theory]
    [InlineData(500, BaseUnit.Gram, "500 g")]
    [InlineData(1000, BaseUnit.Gram, "1 kg")]
    [InlineData(1500, BaseUnit.Gram, "1,5 kg")]
    [InlineData(-2000, BaseUnit.Gram, "-2 kg")]
    [InlineData(250, BaseUnit.Milliliter, "250 ml")]
    [InlineData(1250, BaseUnit.Milliliter, "1,25 l")]
    [InlineData(3, BaseUnit.Piece, "3 Stk.")]
    [InlineData(1200, BaseUnit.Piece, "1200 Stk.")]
    public void Format_German_ScalesToLargerUnitFromThousand(
        int quantity,
        BaseUnit unit,
        string expected
    )
    {
        using var _ = new CultureScope("de-DE");

        Assert.Equal(expected, Units.Format(quantity, unit));
    }

    [Fact]
    public void Format_English_UsesEnglishSeparatorsAndAbbreviations()
    {
        using var _ = new CultureScope("en-US");

        Assert.Equal("1.5 kg", Units.Format(1500m, BaseUnit.Gram));
        Assert.Equal("2 pcs", Units.Format(2m, BaseUnit.Piece));
    }

    [Fact]
    public void Format_TrimsTrailingZerosAndRoundsToThreeDecimals()
    {
        using var _ = new CultureScope("de-DE");

        Assert.Equal("0,333 g", Units.Format(1m / 3m, BaseUnit.Gram));
        Assert.Equal("2 g", Units.Format(2.000m, BaseUnit.Gram));
    }

    [Fact]
    public void PricePerUnit_German_ConvertsBaseUnitPriceToKiloAndLiter()
    {
        using var _ = new CultureScope("de-DE");

        Assert.Equal("2,00 €/kg", Units.PricePerUnit(0.002m, BaseUnit.Gram));
        Assert.Equal("1,19 €/l", Units.PricePerUnit(0.00119m, BaseUnit.Milliliter));
        Assert.Equal("0,50 €/Stk.", Units.PricePerUnit(0.5m, BaseUnit.Piece));
    }

    [Theory]
    [InlineData(BaseUnit.Gram, 2.5)]
    [InlineData(BaseUnit.Milliliter, 2.5)]
    [InlineData(BaseUnit.Piece, 0.0025)]
    public void PerDisplayUnit_ScalesGramAndMilliliterToKiloAndLiter(
        BaseUnit unit,
        double expected
    )
    {
        Assert.Equal((decimal)expected, Units.PerDisplayUnit(0.0025m, unit));
    }

    [Fact]
    public void Money_FollowsCulture()
    {
        using (new CultureScope("de-DE"))
        {
            Assert.Equal("1.234,50 €", Units.Money(1234.5m));
        }

        using (new CultureScope("en-US"))
        {
            Assert.Equal("€1,234.50", Units.Money(1234.5m));
        }
    }

    [Theory]
    [InlineData(BaseUnit.Gram, "g", "Gramm", "kg")]
    [InlineData(BaseUnit.Milliliter, "ml", "Milliliter", "l")]
    [InlineData(BaseUnit.Piece, "Stk.", "Stück", "Stk.")]
    public void Labels_German(
        BaseUnit unit,
        string abbreviation,
        string name,
        string displayAbbreviation
    )
    {
        using var _ = new CultureScope("de-DE");

        Assert.Equal(abbreviation, Units.Abbreviation(unit));
        Assert.Equal(name, Units.Describe(unit));
        Assert.Equal(displayAbbreviation, Units.DisplayAbbreviation(unit));
    }
}
