using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Purchases;

public sealed class ReviewService(IDbContextFactory<HomeBaseDbContext> factory)
{
    public const decimal PromoHintFactor = 0.85m;

    public async Task<IReadOnlyList<ReviewLineRow>> GetLinesAsync(
        long purchaseId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .PurchaseItems.ForPurchase(purchaseId)
            .InReceiptOrder()
            .Select(ReviewLineRow.Projection)
            .ToListAsync(ct);

        var medians = await MediansAsync(db, purchaseId, rows, ct);

        return
        [
            .. rows.Select(row =>
                row.ProductId is { } productId
                && row.PricePerBaseUnit is { } price
                && medians.TryGetValue(productId, out var median)
                && price < median * PromoHintFactor
                    ? row with { PromoHint = true }
                    : row
            ),
        ];
    }

    public async Task<bool> AssignAsync(
        long itemId,
        int productId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var item = await db.PurchaseItems.FindAsync([itemId], ct);

        if (item is null)
        {
            return false;
        }

        var product = await db
            .Products.Where(p => p.Id == productId)
            .Select(p => new { p.PackageSize })
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return false;
        }

        item.ProductId = productId;
        item.MatchStatus = MatchStatus.Manual;
        item.MatchConfidence = 1f;
        item.SuggestedProductId = null;
        item.QuantityBase ??= item.Quantity * product.PackageSize;

        var storeId = await db
            .Purchases.Where(p => p.Id == item.PurchaseId)
            .Select(p => p.StoreId)
            .FirstOrDefaultAsync(ct);

        await LearnAliasAsync(db, item, productId, storeId, ct);

        await db.SaveChangesAsync(ct);

        return true;
    }

    public async Task SetPromoAsync(long itemId, bool isPromo, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.PurchaseItems.Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsPromo, isPromo), ct);
    }

    public async Task<decimal> GetLineSumAsync(long purchaseId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.PurchaseItems.ForPurchase(purchaseId).SumAsync(i => i.LineTotal, ct);
    }

    private static async Task LearnAliasAsync(
        HomeBaseDbContext db,
        PurchaseItem item,
        int productId,
        int? storeId,
        CancellationToken ct
    )
    {
        var normalized = AliasText.Normalize(item.RawText);

        if (normalized.Length == 0)
        {
            return;
        }

        var existing = await db.ProductAliases.ForTextAsync(storeId, normalized, ct);

        if (existing is not null)
        {
            existing.ProductId = productId;
            existing.TimesSeen++;

            return;
        }

        db.ProductAliases.Add(
            new ProductAlias
            {
                ProductId = productId,
                StoreId = storeId,
                RawText = item.RawText ?? normalized,
                NormalizedText = normalized,
                Source = AliasSource.Learned,
            }
        );
    }

    private static async Task<Dictionary<int, decimal>> MediansAsync(
        HomeBaseDbContext db,
        long purchaseId,
        IReadOnlyList<ReviewLineRow> rows,
        CancellationToken ct
    )
    {
        var productIds = rows.Where(r => r.ProductId is not null)
            .Select(r => r.ProductId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return [];
        }

        var history = await db
            .PurchaseItems.ItemLines()
            .Where(i =>
                i.ProductId != null
                && productIds.Contains(i.ProductId.Value)
                && i.PurchaseId != purchaseId
                && !i.IsPromo
                && i.PricePerBaseUnit != null
            )
            .Select(i => new { ProductId = i.ProductId!.Value, Price = i.PricePerBaseUnit!.Value })
            .ToListAsync(ct);

        return history
            .GroupBy(h => h.ProductId)
            .ToDictionary(g => g.Key, g => Median([.. g.Select(h => h.Price)]));
    }

    private static decimal Median(List<decimal> values)
    {
        values.Sort();

        var middle = values.Count / 2;

        return values.Count % 2 == 1
            ? values[middle]
            : (values[middle - 1] + values[middle]) / 2m;
    }
}
