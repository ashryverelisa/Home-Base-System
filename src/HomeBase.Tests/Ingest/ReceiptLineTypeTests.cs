using HomeBase.Database.Enums;
using HomeBase.Features.Ingest;

namespace HomeBase.Tests.Ingest;

public class ReceiptLineTypeTests
{
    private static ReceiptItem Item(decimal lineTotal, string? lineType = null) =>
        new(null, "TEXT", null, lineType, 1m, null, null, lineTotal, null, null, null);

    [Theory]
    [InlineData("Discount", PurchaseLineType.Discount)]
    [InlineData("discount", PurchaseLineType.Discount)]
    [InlineData("DEPOSIT", PurchaseLineType.Deposit)]
    [InlineData("depositreturn", PurchaseLineType.DepositReturn)]
    [InlineData("fee", PurchaseLineType.Fee)]
    public void ResolveLineType_DeclaredType_IsParsedCaseInsensitive(string declared, PurchaseLineType expected)
    {
        Assert.Equal(expected, ReceiptIngestService.ResolveLineType(Item(1m, declared), hasItemAbove: false));
    }

    [Fact]
    public void ResolveLineType_DeclaredTypeWinsOverSign()
    {
        Assert.Equal(
            PurchaseLineType.Item,
            ReceiptIngestService.ResolveLineType(Item(-1m, "Item"), hasItemAbove: true)
        );
    }

    [Fact]
    public void ResolveLineType_NegativeAfterItem_IsDiscount()
    {
        Assert.Equal(PurchaseLineType.Discount, ReceiptIngestService.ResolveLineType(Item(-0.5m), hasItemAbove: true));
    }

    [Fact]
    public void ResolveLineType_NegativeWithoutItemAbove_IsDepositReturn()
    {
        Assert.Equal(
            PurchaseLineType.DepositReturn,
            ReceiptIngestService.ResolveLineType(Item(-0.25m), hasItemAbove: false)
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void ResolveLineType_NoUsableDeclaration_PositiveTotalIsItem(string? declared)
    {
        Assert.Equal(PurchaseLineType.Item, ReceiptIngestService.ResolveLineType(Item(2m, declared), true));
        Assert.Equal(PurchaseLineType.Item, ReceiptIngestService.ResolveLineType(Item(0m, declared), true));
    }
}
