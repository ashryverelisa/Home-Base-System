namespace HomeBase.Features.Inventory;

public static class LowStockRule
{
    public static IReadOnlyList<LowStockRow> Below(
        IReadOnlyList<MinimumStock> minimums,
        IReadOnlyDictionary<int, decimal> stockTotals
    ) =>
        [
            .. minimums
                .Select(m => new LowStockRow(
                    m.ProductId,
                    m.Name,
                    m.BaseUnit,
                    stockTotals.GetValueOrDefault(m.ProductId),
                    m.MinStockBase
                ))
                .Where(r => r.StockBase < r.MinStockBase)
                .OrderBy(r => r.Name),
        ];
}
