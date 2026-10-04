using HomeBase.Localization;

namespace HomeBase.Tests.Localization;

public class LocalizationTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/products", "/products")]
    [InlineData("/stock?zone=fridge", "/stock?zone=fridge")]
    public void LocalOrHome_LocalPath_IsKept(string redirectUri, string expected)
    {
        Assert.Equal(expected, CultureEndpoints.LocalOrHome(redirectUri));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("products")]
    [InlineData("https://evil.example")]
    [InlineData("//evil.example")]
    [InlineData("/\\evil.example")]
    public void LocalOrHome_ExternalOrInvalidTarget_FallsBackToHome(string? redirectUri)
    {
        Assert.Equal("/", CultureEndpoints.LocalOrHome(redirectUri));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-us")]
    public void IsSupported_KnownCultures_CaseInsensitive(string name)
    {
        Assert.True(SupportedCultures.IsSupported(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr-FR")]
    [InlineData("de")]
    public void IsSupported_UnknownCultures_AreRejected(string? name)
    {
        Assert.False(SupportedCultures.IsSupported(name));
    }

    [Fact]
    public void Default_IsGerman()
    {
        Assert.Equal(SupportedCultures.DefaultName, SupportedCultures.Default.Name);
    }

    [Fact]
    public void AppStrings_UnknownKey_FallsBackToKey()
    {
        Assert.Equal("Does.Not.Exist", AppStrings.Get("Does.Not.Exist"));
    }
}
