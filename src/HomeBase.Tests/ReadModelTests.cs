using HomeBase.Database.Enums;
using HomeBase.Features.Assets;
using HomeBase.Features.Catalog;
using HomeBase.Features.Common;
using HomeBase.Features.Ingest;
using HomeBase.Features.Inventory;
using HomeBase.Features.Matching;
using HomeBase.Features.MealPlan;
using HomeBase.Features.Purchases;
using HomeBase.Features.Shopping;

namespace HomeBase.Tests;

public class ReadModelTests
{
    private static readonly DateOnly Today = new(2026, 3, 15);

    private static AssetRow Asset(DateOnly? warrantyUntil = null, DateOnly? nextServiceAt = null) =>
        new(
            1,
            "Waschmaschine",
            null,
            null,
            null,
            null,
            null,
            AssetStatus.InUse,
            null,
            null,
            null,
            warrantyUntil,
            nextServiceAt
        )
        {
            Today = Today,
        };

    private static StockLotView Lot(
        DateOnly? bestBefore,
        decimal quantity = 1m,
        decimal? price = null,
        StorageZone? zone = null
    ) =>
        new(
            1,
            1,
            "Joghurt",
            null,
            BaseUnit.Gram,
            quantity,
            bestBefore,
            null,
            null,
            null,
            zone,
            price
        )
        {
            Today = Today,
        };

    [Theory]
    [InlineData(-1, true, false)]
    [InlineData(0, false, true)]
    [InlineData(AssetRow.WarrantyWarningDays, false, true)]
    [InlineData(AssetRow.WarrantyWarningDays + 1, false, false)]
    public void AssetRow_WarrantyState(int daysFromToday, bool expired, bool endingSoon)
    {
        var asset = Asset(warrantyUntil: Today.AddDays(daysFromToday));

        Assert.Equal(daysFromToday, asset.WarrantyDaysLeft);
        Assert.Equal(expired, asset.WarrantyExpired);
        Assert.Equal(endingSoon, asset.WarrantyEndingSoon);
    }

    [Fact]
    public void AssetRow_WithoutDates_HasNoWarrantyOrServiceFlags()
    {
        var asset = Asset();

        Assert.Null(asset.WarrantyDaysLeft);
        Assert.False(asset.WarrantyExpired);
        Assert.False(asset.WarrantyEndingSoon);
        Assert.False(asset.ServiceDue);
    }

    [Theory]
    [InlineData(-3, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void AssetRow_ServiceDue_OnOrAfterDate(int daysFromToday, bool expected)
    {
        Assert.Equal(expected, Asset(nextServiceAt: Today.AddDays(daysFromToday)).ServiceDue);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    public void StockLotView_ExpiredOnlyAfterBestBeforeDay(int daysFromToday, bool expected)
    {
        var lot = Lot(Today.AddDays(daysFromToday));

        Assert.Equal(daysFromToday, lot.DaysLeft);
        Assert.Equal(expected, lot.IsExpired);
    }

    [Fact]
    public void StockLotView_WithoutBestBefore_NeverExpires()
    {
        Assert.False(Lot(null).IsExpired);
        Assert.False(Lot(null).ExpiresSoon);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(StockLotView.ExpiryWarningDays, true)]
    [InlineData(StockLotView.ExpiryWarningDays + 1, false)]
    public void StockLotView_ExpiresSoon_FromTodayUntilWarningDays(int daysFromToday, bool expected)
    {
        Assert.Equal(expected, Lot(Today.AddDays(daysFromToday)).ExpiresSoon);
    }

    [Theory]
    [InlineData(StockLotView.ExpiryWarningDays + 1, true)]
    [InlineData(Zones.FreezerWarningDays, true)]
    [InlineData(Zones.FreezerWarningDays + 1, false)]
    public void StockLotView_InFreezer_WarnsWithLongerLeadTime(int daysFromToday, bool expected)
    {
        var lot = Lot(Today.AddDays(daysFromToday), zone: StorageZone.Freezer);

        Assert.Equal(expected, lot.ExpiresSoon);
    }

    [Fact]
    public void StockLotView_ExpiredInFreezer_IsExpiredNotSoon()
    {
        var lot = Lot(Today.AddDays(-1), zone: StorageZone.Freezer);

        Assert.True(lot.IsExpired);
        Assert.False(lot.ExpiresSoon);
        Assert.True(lot.IsDueWithin(7));
    }

    [Theory]
    [InlineData(StorageZone.Fridge, 7, true)]
    [InlineData(StorageZone.Fridge, 8, false)]
    [InlineData(StorageZone.Freezer, 8, true)]
    public void StockLotView_IsDueWithin_RespectsZone(StorageZone zone, int daysFromToday, bool expected)
    {
        Assert.Equal(expected, Lot(Today.AddDays(daysFromToday), zone: zone).IsDueWithin(7));
    }

    [Fact]
    public void StockLotView_Value_IsPriceTimesQuantity()
    {
        Assert.Equal(1.50m, Lot(null, quantity: 500m, price: 0.003m).Value);
        Assert.Null(Lot(null, quantity: 500m).Value);
    }

    [Theory]
    [InlineData(100, 200, true)]
    [InlineData(200, 200, false)]
    [InlineData(300, 200, false)]
    public void ProductRow_IsBelowMinimum(int stock, int minimum, bool expected)
    {
        var row = new ProductRow(1, "Reis", null, null, BaseUnit.Gram, 1000m, true, null, minimum)
        {
            StockBase = stock,
        };

        Assert.Equal(expected, row.IsBelowMinimum);
    }

    [Fact]
    public void ProductRow_WithoutMinimum_IsNeverBelow()
    {
        var row = new ProductRow(1, "Reis", null, null, BaseUnit.Gram, 1000m, true, null, null)
        {
            StockBase = 0m,
        };

        Assert.False(row.IsBelowMinimum);
    }

    [Fact]
    public void NeedRow_MissingIsNeverNegative()
    {
        Assert.Equal(0m, new NeedRow(1, "Mehl", BaseUnit.Gram, 100m, 500m).Missing);
        Assert.Equal(400m, new NeedRow(1, "Mehl", BaseUnit.Gram, 500m, 100m).Missing);
    }

    [Theory]
    [InlineData("Pfannkuchen", "frei", "Pfannkuchen")]
    [InlineData(null, "Reste", "Reste")]
    [InlineData(null, null, "")]
    public void MealPlanRow_Label_PrefersRecipeName(
        string? recipe,
        string? freeText,
        string expected
    )
    {
        var row = new MealPlanRow(
            1,
            Today,
            MealSlot.Dinner,
            null,
            recipe,
            freeText,
            2,
            MealPlanStatus.Planned,
            null
        );

        Assert.Equal(expected, row.Label);
        Assert.True(row.IsPlanned);
        Assert.False(row.IsCooked);
    }

    [Theory]
    [InlineData(PurchaseLineType.Item, null, true)]
    [InlineData(PurchaseLineType.Item, 5, false)]
    [InlineData(PurchaseLineType.Discount, null, false)]
    [InlineData(PurchaseLineType.Deposit, null, false)]
    public void ReviewLineRow_NeedsAssignment_OnlyForUnmatchedItems(
        PurchaseLineType type,
        int? productId,
        bool expected
    )
    {
        var row = new ReviewLineRow(
            1,
            1,
            type,
            "TEXT",
            null,
            productId,
            null,
            null,
            null,
            MatchStatus.Unmatched,
            null,
            1m,
            null,
            1m,
            null,
            false,
            null
        );

        Assert.Equal(expected, row.NeedsAssignment);
    }

    [Fact]
    public void ShoppingItemRow_Flags()
    {
        var row = new ShoppingItemRow(
            1,
            1,
            null,
            null,
            null,
            "Batterien",
            null,
            4m,
            "Stk",
            Priority: 1,
            null,
            ShoppingListItemStatus.Open,
            ShoppingListItemOrigin.AutoRestock,
            null
        );

        Assert.Equal("Batterien", row.Label);
        Assert.True(row.IsOpen);
        Assert.True(row.IsAutomatic);
        Assert.True(row.IsImportant);
    }

    [Fact]
    public void ProductMatch_None_IsUnmatched()
    {
        Assert.False(ProductMatch.None.IsMatched);
        Assert.Equal(MatchStatus.Unmatched, ProductMatch.None.Status);
        Assert.True(new ProductMatch(3, MatchStatus.Gtin, 1f, null).IsMatched);
    }

    [Theory]
    [InlineData(5, 10, true)]
    [InlineData(null, 10, false)]
    [InlineData(5, 0, false)]
    [InlineData(5, null, false)]
    public void IngredientLine_IsTracked_NeedsProductAndPositiveQuantity(
        int? productId,
        int? quantity,
        bool expected
    )
    {
        var line = new IngredientLine(1, productId, "X", BaseUnit.Gram, quantity, null, false);

        Assert.Equal(expected, line.IsTracked);
    }
}
