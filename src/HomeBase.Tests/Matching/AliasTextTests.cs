using HomeBase.Features.Matching;

namespace HomeBase.Tests.Matching;

public class AliasTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_BlankInput_ReturnsEmpty(string? raw)
    {
        Assert.Equal(string.Empty, AliasText.Normalize(raw));
    }

    [Theory]
    [InlineData("MILCH", "milch")]
    [InlineData("  Milch 3,5% 1L  ", "milch 3 5 1l")]
    [InlineData("Käse-Aufschnitt", "käse aufschnitt")]
    [InlineData("BIO--Äpfel!!", "bio äpfel")]
    [InlineData("Tomaten\t\tpassiert", "tomaten passiert")]
    public void Normalize_CollapsesSeparatorsAndLowercases(string raw, string expected)
    {
        Assert.Equal(expected, AliasText.Normalize(raw));
    }

    [Fact]
    public void Normalize_SameProductWithDifferentFormatting_YieldsSameKey()
    {
        Assert.Equal(AliasText.Normalize("Ja! Butter 250g"), AliasText.Normalize("JA BUTTER  250G"));
    }

    [Theory]
    [InlineData("Rabatt Milch")]
    [InlineData("aktion joghurt")]
    [InlineData("Angebot: Kaffee")]
    [InlineData("COUPON 0,50")]
    [InlineData("Milch -20%")]
    [InlineData("Butter - 1,5 %")]
    [InlineData("10 % Nachlass")]
    public void LooksPromotional_PromoText_ReturnsTrue(string raw)
    {
        Assert.True(AliasText.LooksPromotional(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Vollmilch 3,5%")]
    [InlineData("Joghurt Natur")]
    public void LooksPromotional_RegularText_ReturnsFalse(string? raw)
    {
        Assert.False(AliasText.LooksPromotional(raw));
    }
}
