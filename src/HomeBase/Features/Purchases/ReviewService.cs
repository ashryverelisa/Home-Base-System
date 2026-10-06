using HomeBase.Database;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Ingest;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Purchases;

public sealed class ReviewService(IDbContextFactory<HomeBaseDbContext> factory) : IReviewService
{
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

        return PromoDetection.Apply(rows, medians);
    }

    public async Task<bool> AssignAsync(long itemId, int productId, CancellationToken ct = default)
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

        await AliasLearning.LearnAsync(db, item.RawText, productId, storeId, ct);

        await db.SaveChangesAsync(ct);

        return true;
    }

    public async Task SetPromoAsync(long itemId, bool isPromo, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db
            .PurchaseItems.Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsPromo, isPromo), ct);
    }

    public async Task<decimal> GetLineSumAsync(long purchaseId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.PurchaseItems.ForPurchase(purchaseId).SumAsync(i => i.LineTotal, ct);
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
            .Select(i => new ProductPrice(i.ProductId!.Value, i.PricePerBaseUnit!.Value))
            .ToListAsync(ct);

        return PromoDetection.MediansByProduct(history);
    }
}
