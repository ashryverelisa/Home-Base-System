using HomeBase.Database.Enums;
using HomeBase.Features.Purchases;

namespace HomeBase.Tests.Purchases;

public class PurchaseServiceToItemTests
{
    [Fact]
    public void ToItem_MatchedItem_KeepsProductAndComputesUnitPrice()
    {
        var line = new PurchaseDraftLine
        {
            ProductId = 5,
            RawText = "  BUTTER 250G ",
            Quantity = 2m,
            QuantityBase = 500m,
            LineTotal = 3.98m,
            IsPromo = true,
        };

        var item = PurchaseService.ToItem(line, lineNo: 3);

        Assert.Equal(3, item.LineNo);
        Assert.Equal(PurchaseLineType.Item, item.LineType);
        Assert.Equal(5, item.ProductId);
        Assert.Equal("BUTTER 250G", item.RawText);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(500m, item.QuantityBase);
        Assert.Equal(3.98m, item.LineTotal);
        Assert.Equal(1.99m, item.UnitPrice);
        Assert.True(item.IsPromo);
        Assert.Equal(MatchStatus.Manual, item.MatchStatus);
    }

    [Fact]
    public void ToItem_ItemWithoutProduct_IsUnmatched()
    {
        var item = PurchaseService.ToItem(new PurchaseDraftLine { RawText = "???", LineTotal = 1m }, 1);

        Assert.Null(item.ProductId);
        Assert.Equal(MatchStatus.Unmatched, item.MatchStatus);
    }

    [Fact]
    public void ToItem_DiscountLine_IsNegativeAndDropsProductData()
    {
        var line = new PurchaseDraftLine
        {
            LineType = PurchaseLineType.Discount,
            ProductId = 5,
            Quantity = 3m,
            QuantityBase = 750m,
            LineTotal = 0.30m,
        };

        var item = PurchaseService.ToItem(line, 2);

        Assert.Null(item.ProductId);
        Assert.Null(item.QuantityBase);
        Assert.Equal(1m, item.Quantity);
        Assert.Equal(-0.30m, item.LineTotal);
        Assert.Equal(-0.30m, item.UnitPrice);
        Assert.Equal(MatchStatus.Unmatched, item.MatchStatus);
    }

    [Fact]
    public void ToItem_ZeroQuantity_HasNoUnitPrice()
    {
        var item = PurchaseService.ToItem(new PurchaseDraftLine { ProductId = 1, Quantity = 0m, LineTotal = 2m }, 1);

        Assert.Null(item.UnitPrice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToItem_BlankRawText_IsStoredAsNull(string? rawText)
    {
        Assert.Null(PurchaseService.ToItem(new PurchaseDraftLine { RawText = rawText }, 1).RawText);
    }
}
