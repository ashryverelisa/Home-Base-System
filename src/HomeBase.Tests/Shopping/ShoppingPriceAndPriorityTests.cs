using HomeBase.Database.Enums;
using HomeBase.Features.Shopping;

namespace HomeBase.Tests.Shopping;

public class ShoppingPriceAndPriorityTests
{
    [Theory]
    [InlineData(-5, ShoppingPriority.Low)]
    [InlineData(-1, ShoppingPriority.Low)]
    [InlineData(0, ShoppingPriority.Normal)]
    [InlineData(1, ShoppingPriority.High)]
    [InlineData(2, ShoppingPriority.Urgent)]
    [InlineData(9, ShoppingPriority.Urgent)]
    public void FromValue_ClampsToKnownLevels(int value, ShoppingPriority expected)
    {
        Assert.Equal(expected, ShoppingPriorities.FromValue(value));
    }

    [Fact]
    public void All_IsOrderedMostUrgentFirst()
    {
        Assert.Equal(
            ShoppingPriorities.All.OrderByDescending(p => (int)p),
            ShoppingPriorities.All
        );
    }

    [Theory]
    [InlineData(ShoppingListKind.Groceries, false)]
    [InlineData(ShoppingListKind.Tech, true)]
    [InlineData(ShoppingListKind.Household, true)]
    [InlineData(ShoppingListKind.Other, true)]
    public void TracksPrices_OnlyForNonGroceryLists(ShoppingListKind kind, bool expected)
    {
        Assert.Equal(expected, new ShoppingListRow(1, "Liste", kind, false).TracksPrices);
    }

    [Fact]
    public void NormalizePrice_RoundsToCents()
    {
        Assert.Equal(19.99m, ShoppingService.NormalizePrice(19.989m));
        Assert.Equal(0.01m, ShoppingService.NormalizePrice(0.005m));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-3)]
    public void NormalizePrice_EmptyOrNonPositive_ClearsPrice(int? price)
    {
        Assert.Null(ShoppingService.NormalizePrice(price));
    }

    [Theory]
    [InlineData("https://example.com/item/42", "https://example.com/item/42")]
    [InlineData("  http://shop.de/a?b=c  ", "http://shop.de/a?b=c")]
    [InlineData("www.amazon.de/dp/B0123", "https://www.amazon.de/dp/B0123")]
    public void TryNormalizeLink_AcceptsWebAddresses(string input, string expected)
    {
        Assert.True(ShoppingService.TryNormalizeLink(input, out var link));
        Assert.Equal(expected, link);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalizeLink_Empty_ClearsLink(string? input)
    {
        Assert.True(ShoppingService.TryNormalizeLink(input, out var link));
        Assert.Null(link);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("not a link")]
    public void TryNormalizeLink_RejectsNonWebLinks(string input)
    {
        Assert.False(ShoppingService.TryNormalizeLink(input, out _));
    }
}
