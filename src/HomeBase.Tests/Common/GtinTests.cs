using HomeBase.Features.Common;

namespace HomeBase.Tests.Common;

public class GtinTests
{
    [Theory]
    [InlineData("4006381333931")]
    [InlineData("3017624010701")]
    [InlineData("96385074")]
    [InlineData("036000291452")]
    [InlineData("10614141000415")]
    public void IsValid_CorrectCheckDigit_IsAccepted(string gtin)
    {
        Assert.True(Gtin.IsValid(gtin));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4006381333932")]
    [InlineData("400638133393")]
    [InlineData("40063813339311")]
    [InlineData("400638133393a")]
    [InlineData("12345")]
    public void IsValid_WrongLengthDigitOrCheckDigit_IsRejected(string? gtin)
    {
        Assert.False(Gtin.IsValid(gtin));
    }

    [Theory]
    [InlineData(" 4006381333931 ", "4006381333931")]
    [InlineData("4 006381 333931", "4006381333931")]
    [InlineData("4006381-333931", "4006381333931")]
    public void Normalize_StripsSpacesAndDashes(string input, string expected)
    {
        Assert.Equal(expected, Gtin.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("Milch")]
    [InlineData("4006381333932")]
    public void Normalize_NoValidGtin_ReturnsNull(string? input)
    {
        Assert.Null(Gtin.Normalize(input));
    }
}
