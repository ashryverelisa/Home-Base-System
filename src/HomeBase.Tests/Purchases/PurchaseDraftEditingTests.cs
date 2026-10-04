using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Purchases;

namespace HomeBase.Tests.Purchases;

public class PurchaseDraftEditingTests
{
    private static readonly ProductRow Butter = new(
        5,
        "Butter",
        null,
        null,
        BaseUnit.Gram,
        250m,
        true,
        null,
        null
    );

    private static PurchaseDraftLine Item(string name) =>
        new()
        {
            ProductId = 1,
            ProductName = name,
            LineTotal = 1m,
        };

    private static PurchaseDraftLine Discount(int? parentIndex) =>
        new()
        {
            LineType = PurchaseLineType.Discount,
            RawText = "Rabatt",
            LineTotal = 0.2m,
            ParentIndex = parentIndex,
        };

    [Fact]
    public void ItemLineIndexes_ListsOnlyItemPositions()
    {
        var draft = new PurchaseDraft();
        draft.Lines.AddRange([Item("A"), Discount(0), Item("B"), Discount(2)]);

        Assert.Equal([0, 2], draft.ItemLineIndexes);
    }

    [Fact]
    public void RemoveLine_ShiftsParentIndexesBehindRemovedLine()
    {
        var draft = new PurchaseDraft();
        var first = Item("A");
        var second = Item("B");
        var discountOnSecond = Discount(1);
        draft.Lines.AddRange([first, second, discountOnSecond]);

        draft.RemoveLine(first);

        Assert.Equal([second, discountOnSecond], draft.Lines);
        Assert.Equal(0, discountOnSecond.ParentIndex);
        Assert.Same(second, draft.Lines[discountOnSecond.ParentIndex!.Value]);
    }

    [Fact]
    public void RemoveLine_RemovedParent_UnlinksItsDiscounts()
    {
        var draft = new PurchaseDraft();
        var item = Item("A");
        var discount = Discount(0);
        draft.Lines.AddRange([item, discount]);

        draft.RemoveLine(item);

        Assert.Null(discount.ParentIndex);
    }

    [Fact]
    public void RemoveLine_ParentBeforeRemovedLine_StaysUnchanged()
    {
        var draft = new PurchaseDraft();
        var discount = Discount(0);
        var other = Item("B");
        draft.Lines.AddRange([Item("A"), discount, other]);

        draft.RemoveLine(other);

        Assert.Equal(0, discount.ParentIndex);
    }

    [Fact]
    public void RemoveLine_UnknownLine_ChangesNothing()
    {
        var draft = new PurchaseDraft();
        var discount = Discount(0);
        draft.Lines.AddRange([Item("A"), discount]);

        draft.RemoveLine(Item("fremd"));

        Assert.Equal(2, draft.Lines.Count);
        Assert.Equal(0, discount.ParentIndex);
    }

    [Fact]
    public void ChangeType_AwayFromItem_ClearsProductData()
    {
        var line = new PurchaseDraftLine
        {
            ProductId = 5,
            ProductName = "Butter",
            BaseUnit = BaseUnit.Gram,
            QuantityBase = 250m,
            IsPromo = true,
            BestBefore = new DateOnly(2026, 1, 1),
            RawText = "Pfand",
            LineTotal = 0.25m,
        };

        line.ChangeType(PurchaseLineType.Deposit);

        Assert.Equal(PurchaseLineType.Deposit, line.LineType);
        Assert.Null(line.ProductId);
        Assert.Null(line.ProductName);
        Assert.Null(line.BaseUnit);
        Assert.Null(line.QuantityBase);
        Assert.False(line.IsPromo);
        Assert.Null(line.BestBefore);
        Assert.Equal("Pfand", line.RawText);
        Assert.Equal(0.25m, line.LineTotal);
    }

    [Fact]
    public void ChangeType_AwayFromDiscount_ClearsParent()
    {
        var line = Discount(3);

        line.ChangeType(PurchaseLineType.Fee);

        Assert.Null(line.ParentIndex);
    }

    [Fact]
    public void ChangeType_ToDiscount_KeepsParent()
    {
        var line = Discount(3);

        line.ChangeType(PurchaseLineType.Discount);

        Assert.Equal(3, line.ParentIndex);
    }

    [Fact]
    public void SelectProduct_TakesOverProductAndOnePackage()
    {
        var line = new PurchaseDraftLine { Quantity = 4m };

        line.SelectProduct(Butter);

        Assert.Equal(5, line.ProductId);
        Assert.Equal("Butter", line.ProductName);
        Assert.Equal(BaseUnit.Gram, line.BaseUnit);
        Assert.Equal(1m, line.Quantity);
        Assert.Equal(250m, line.QuantityBase);
    }

    [Fact]
    public void SelectProduct_Null_ClearsProduct()
    {
        var line = new PurchaseDraftLine();
        line.SelectProduct(Butter);

        line.SelectProduct(null);

        Assert.Null(line.ProductId);
        Assert.Null(line.ProductName);
        Assert.Null(line.QuantityBase);
    }

    [Fact]
    public void ChangeQuantity_WithPackageSize_RecalculatesBaseQuantity()
    {
        var line = new PurchaseDraftLine();

        line.ChangeQuantity(3m, packageSize: 250m);

        Assert.Equal(3m, line.Quantity);
        Assert.Equal(750m, line.QuantityBase);
    }

    [Fact]
    public void ChangeQuantity_WithoutPackageSize_KeepsManualBaseQuantity()
    {
        var line = new PurchaseDraftLine { QuantityBase = 1234m };

        line.ChangeQuantity(2m, packageSize: null);

        Assert.Equal(2m, line.Quantity);
        Assert.Equal(1234m, line.QuantityBase);
    }

    [Fact]
    public void Validate_CompleteItem_IsValid()
    {
        Assert.Null(Item("A").Validate());
    }

    [Fact]
    public void Validate_ItemWithoutProduct_RequiresProduct()
    {
        Assert.Equal(
            "Purchases.ProductRequired",
            new PurchaseDraftLine { LineTotal = 1m }.Validate()
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_NonItemWithoutText_RequiresText(string? rawText)
    {
        var line = Discount(null);
        line.RawText = rawText;

        Assert.Equal("Purchases.TextRequired", line.Validate());
    }

    [Fact]
    public void Validate_ZeroAmount_RequiresAmount()
    {
        var line = Item("A");
        line.LineTotal = 0m;

        Assert.Equal("Purchases.AmountRequired", line.Validate());
    }

    [Fact]
    public void Validate_ErrorKeysExistInResources()
    {
        using var _ = new CultureScope("de-DE");

        string[] keys =
        [
            "Purchases.ProductRequired",
            "Purchases.TextRequired",
            "Purchases.AmountRequired",
        ];

        Assert.All(keys, key => Assert.NotEqual(key, HomeBase.Localization.AppStrings.Get(key)));
    }
}
