using HomeBase.Features.Ingest;

namespace HomeBase.Tests.Ingest;

public class RecipeIngredientParserTests
{
    private static RecipeIngestService.ParsedIngredient Parse(
        string? text,
        decimal? quantity = null,
        string? unit = null,
        string? name = null
    ) => RecipeIngestService.Parse(new RecipeIngredientRequest(text, quantity, unit, name, null, null));

    [Theory]
    [InlineData("200 g Mehl", 200, "g", "Mehl")]
    [InlineData("500g Hackfleisch", 500, "g", "Hackfleisch")]
    [InlineData("1,5 kg Kartoffeln", 1.5, "kg", "Kartoffeln")]
    [InlineData("0.5 l Milch", 0.5, "l", "Milch")]
    [InlineData("250 ml Sahne", 250, "ml", "Sahne")]
    [InlineData("2 EL Olivenöl", 2, "EL", "Olivenöl")]
    [InlineData("1 TL Zimt", 1, "TL", "Zimt")]
    [InlineData("3 Stk. Zwiebeln", 3, "Stk", "Zwiebeln")]
    [InlineData("1 Prise Salz", 1, "Prise", "Salz")]
    [InlineData("1 Packung Backpulver", 1, "Packung", "Backpulver")]
    public void Parse_QuantityUnitAndName(string text, double quantity, string unit, string name)
    {
        var parsed = Parse(text);

        Assert.Equal(name, parsed.Name);
        Assert.Equal((decimal)quantity, parsed.Quantity);
        Assert.Equal(unit, parsed.Unit);
    }

    [Fact]
    public void Parse_CountWithoutUnit_LeavesUnitEmpty()
    {
        var parsed = Parse("2 Eier");

        Assert.Equal(new RecipeIngestService.ParsedIngredient("Eier", 2m, null), parsed);
    }

    [Fact]
    public void Parse_UnitLetterAtStartOfWord_IsNotMistakenForUnit()
    {
        var parsed = Parse("2 große Zwiebeln");

        Assert.Equal(new RecipeIngestService.ParsedIngredient("große Zwiebeln", 2m, null), parsed);
    }

    [Theory]
    [InlineData("Salz")]
    [InlineData("Salz und Pfeffer")]
    public void Parse_TextWithoutQuantity_UsesWholeTextAsName(string text)
    {
        var parsed = Parse(text);

        Assert.Equal(text, parsed.Name);
        Assert.Null(parsed.Quantity);
    }

    [Fact]
    public void Parse_ExplicitQuantity_WinsOverParsedQuantity()
    {
        Assert.Equal(250m, Parse("200 g Mehl", quantity: 250m).Quantity);
    }

    [Fact]
    public void Parse_UnitOnlyInRequest_IsUsedWhenTextHasNone()
    {
        Assert.Equal("g", Parse("2 Zucker", unit: " g ").Unit);
    }

    [Fact]
    public void Parse_StructuredName_SkipsTextParsing()
    {
        var parsed = Parse("200 g Mehl", quantity: 1m, unit: " kg ", name: " Weizenmehl ");

        Assert.Equal(new RecipeIngestService.ParsedIngredient("Weizenmehl", 1m, "kg"), parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NoTextAndNoName_YieldsEmptyName(string? text)
    {
        Assert.Equal(string.Empty, Parse(text).Name);
    }
}
