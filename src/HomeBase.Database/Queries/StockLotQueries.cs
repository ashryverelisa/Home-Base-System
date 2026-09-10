using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class StockLotQueries
{
    public static IQueryable<StockLot> InStock(this IQueryable<StockLot> lots) =>
        lots.Where(l => l.QuantityBase > 0);

    public static IQueryable<StockLot> ForProduct(this IQueryable<StockLot> lots, int productId) =>
        lots.Where(l => l.ProductId == productId);

    public static IQueryable<StockLot> InLocations(
        this IQueryable<StockLot> lots,
        IReadOnlyCollection<int> locationIds
    ) => lots.Where(l => l.LocationId != null && locationIds.Contains(l.LocationId.Value));

    public static IQueryable<StockLot> BestBeforeUntil(
        this IQueryable<StockLot> lots,
        DateOnly until
    ) => lots.Where(l => l.BestBefore != null && l.BestBefore <= until);

    public static IOrderedQueryable<StockLot> FirstExpiredFirstOut(
        this IQueryable<StockLot> lots
    ) => lots.OrderBy(l => l.BestBefore == null).ThenBy(l => l.BestBefore).ThenBy(l => l.CreatedAt);

    public static Task<Dictionary<int, decimal>> StockTotalsByProductAsync(
        this IQueryable<StockLot> lots,
        CancellationToken ct = default
    ) =>
        lots.InStock()
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(l => l.QuantityBase) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total, ct);

    public static async Task<bool> TryDeductAsync(
        this IQueryable<StockLot> lots,
        long lotId,
        decimal quantityBase,
        CancellationToken ct = default
    ) =>
        await lots.Where(l => l.Id == lotId && l.QuantityBase >= quantityBase)
            .ExecuteUpdateAsync(
                s => s.SetProperty(l => l.QuantityBase, l => l.QuantityBase - quantityBase),
                ct
            ) == 1;

    public static async Task<bool> MarkOpenedAsync(
        this IQueryable<StockLot> lots,
        long lotId,
        DateTimeOffset openedAt,
        CancellationToken ct = default
    ) =>
        await lots.Where(l => l.Id == lotId && l.OpenedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.OpenedAt, openedAt), ct) == 1;
}
