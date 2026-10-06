using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class PurchaseQueries
{
    public static IQueryable<Purchase> Confirmed(this IQueryable<Purchase> purchases) =>
        purchases.Where(p => p.Status == PurchaseStatus.Confirmed);

    public static IQueryable<Purchase> Pending(this IQueryable<Purchase> purchases) =>
        purchases.Where(p =>
            p.Status == PurchaseStatus.Draft || p.Status == PurchaseStatus.NeedsReview
        );

    public static IOrderedQueryable<Purchase> NewestFirst(this IQueryable<Purchase> purchases) =>
        purchases.OrderByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id);

    public static IQueryable<Purchase> Since(
        this IQueryable<Purchase> purchases,
        DateTimeOffset from
    ) => purchases.Where(p => p.PurchasedAt >= from);

    public static IQueryable<PurchaseItem> ForPurchase(
        this IQueryable<PurchaseItem> items,
        long purchaseId
    ) => items.Where(i => i.PurchaseId == purchaseId);

    public static IQueryable<PurchaseItem> ItemLines(this IQueryable<PurchaseItem> items) =>
        items.Where(i => i.LineType == PurchaseLineType.Item);

    public static IOrderedQueryable<PurchaseItem> InReceiptOrder(
        this IQueryable<PurchaseItem> items
    ) => items.OrderBy(i => i.LineNo);

    // All lines, and the item lines that still wait for a product.
    public static Task<Dictionary<long, (int Lines, int Unmatched)>> LineCountsAsync(
        this IQueryable<PurchaseItem> items,
        IReadOnlyCollection<long> purchaseIds,
        CancellationToken ct = default
    ) =>
        items
            .Where(i => purchaseIds.Contains(i.PurchaseId))
            .GroupBy(i => i.PurchaseId)
            .Select(g => new
            {
                PurchaseId = g.Key,
                Lines = g.Count(),
                Unmatched = g.Count(i =>
                    i.LineType == PurchaseLineType.Item && i.ProductId == null
                ),
            })
            .ToDictionaryAsync(x => x.PurchaseId, x => (x.Lines, x.Unmatched), ct);
}
