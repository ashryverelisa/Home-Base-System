using HomeBase.Database.Enums;
using HomeBase.Features.Inventory;

namespace HomeBase.Tests.Inventory;

public class LowStockRuleTests
{
    [Fact]
    public void Below_ReturnsOnlyProductsUnderTheirMinimum()
    {
        MinimumStock[] minimums =
        [
            new(1, "Milch", BaseUnit.Milliliter, 2000m),
            new(2, "Mehl", BaseUnit.Gram, 1000m),
        ];

        var rows = LowStockRule.Below(minimums, new Dictionary<int, decimal> { [1] = 500m, [2] = 1500m });

        var row = Assert.Single(rows);
        Assert.Equal(new LowStockRow(1, "Milch", BaseUnit.Milliliter, 500m, 2000m), row);
    }

    [Fact]
    public void Below_StockExactlyAtMinimum_IsNotLow()
    {
        MinimumStock[] minimums = [new(1, "Eier", BaseUnit.Piece, 6m)];

        Assert.Empty(LowStockRule.Below(minimums, new Dictionary<int, decimal> { [1] = 6m }));
    }

    [Fact]
    public void Below_ProductWithoutStockEntry_CountsAsZero()
    {
        MinimumStock[] minimums = [new(4, "Butter", BaseUnit.Gram, 250m)];

        var row = Assert.Single(LowStockRule.Below(minimums, new Dictionary<int, decimal>()));
        Assert.Equal(0m, row.StockBase);
    }

    [Fact]
    public void Below_SortsByName()
    {
        MinimumStock[] minimums =
        [
            new(1, "Zucker", BaseUnit.Gram, 1m),
            new(2, "Apfelsaft", BaseUnit.Milliliter, 1m),
            new(3, "Kaffee", BaseUnit.Gram, 1m),
        ];

        var rows = LowStockRule.Below(minimums, new Dictionary<int, decimal>());

        Assert.Equal(["Apfelsaft", "Kaffee", "Zucker"], rows.Select(r => r.Name));
    }
}
