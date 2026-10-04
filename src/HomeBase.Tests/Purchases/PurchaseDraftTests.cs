using HomeBase.Database.Enums;
using HomeBase.Features.Purchases;

namespace HomeBase.Tests.Purchases;

public class PurchaseDraftTests
{
    [Theory]
    [InlineData(PurchaseLineType.Item, 2.49, 2.49)]
    [InlineData(PurchaseLineType.Item, -2.49, 2.49)]
    [InlineData(PurchaseLineType.Deposit, 0.25, 0.25)]
    [InlineData(PurchaseLineType.Fee, 0.10, 0.10)]
    [InlineData(PurchaseLineType.Discount, 0.50, -0.50)]
    [InlineData(PurchaseLineType.Discount, -0.50, -0.50)]
    [InlineData(PurchaseLineType.DepositReturn, 0.25, -0.25)]
    public void SignedTotal_CreditsAreNegativeEverythingElsePositive(
        PurchaseLineType type,
        double entered,
        double expected
    )
    {
        var line = new PurchaseDraftLine { LineType = type, LineTotal = (decimal)entered };

        Assert.Equal((decimal)expected, line.SignedTotal);
    }

    [Fact]
    public void LineSum_SubtractsDiscountsAndDepositReturns()
    {
        var draft = new PurchaseDraft();
        draft.Lines.Add(new PurchaseDraftLine { LineTotal = 3.00m });
        draft.Lines.Add(new PurchaseDraftLine { LineType = PurchaseLineType.Deposit, LineTotal = 0.25m });
        draft.Lines.Add(new PurchaseDraftLine { LineType = PurchaseLineType.Discount, LineTotal = 0.50m });
        draft.Lines.Add(new PurchaseDraftLine { LineType = PurchaseLineType.DepositReturn, LineTotal = -1.00m });

        Assert.Equal(1.75m, draft.LineSum);
    }

    [Fact]
    public void Label_PrefersProductNameOverRawText()
    {
        Assert.Equal("Milch", new PurchaseDraftLine { ProductName = "Milch", RawText = "MILCH 1L" }.Label);
        Assert.Equal("MILCH 1L", new PurchaseDraftLine { RawText = "MILCH 1L" }.Label);
        Assert.Equal(string.Empty, new PurchaseDraftLine().Label);
    }
}
