using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using HomeBase.Features.Matching;
using HomeBase.Features.Purchases;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Ingest;

public sealed record ReceiptIngestResult(
    long PurchaseId,
    PurchaseStatus Status,
    int Matched,
    int Unmatched,
    bool WasKnown
);

public sealed class ReceiptIngestService(
    IDbContextFactory<HomeBaseDbContext> factory,
    TimeProvider time
) : IReceiptIngestService
{
    public async Task<ReceiptIngestResult?> IngestAsync(
        ReceiptRequest request,
        CancellationToken ct = default
    )
    {
        if (request.Items is not { Count: > 0 } incoming)
        {
            return null;
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        if (
            request.ExternalId is { Length: > 0 } externalId
            && await ReadKnownAsync(db, externalId, ct) is { } known
        )
        {
            return known;
        }

        var storeId = request.Store is { } store
            ? await StoreResolver.FindOrCreateAsync(db, store.Name, store.TaxId, ct)
            : null;

        var purchase = new Purchase
        {
            StoreId = storeId,
            PurchasedAt = request.PurchasedAt ?? time.GetUtcNow(),
            Total = request.Total,
            Currency = request.Currency.TrimToNull() ?? "EUR",
            PaymentMethod = request.PaymentMethod,
            Source = PurchaseSource.N8nReceipt,
            ExternalId = request.ExternalId,
            Status = PurchaseStatus.Draft,
            RawPayload = request.Raw?.GetRawText(),
        };

        var lines = await BuildLinesAsync(db, storeId, incoming, ct);

        purchase.Items.AddRange(lines.Select(l => l.Item));

        var sum = purchase.Items.Sum(i => i.LineTotal);
        var unmatched = lines.Count(l => l.NeedsReview);
        var totalMismatch = request.Total is { } total && total != sum;

        purchase.Total ??= sum;
        purchase.Status =
            unmatched > 0 || totalMismatch ? PurchaseStatus.NeedsReview : PurchaseStatus.Draft;

        db.Purchases.Add(purchase);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (request.ExternalId is { Length: > 0 })
        {
            // A retry from n8n raced us; the unique external_id kept the receipt single.
            await using var fresh = await factory.CreateDbContextAsync(ct);

            return await ReadKnownAsync(fresh, request.ExternalId, ct);
        }

        await DiscountLinker.LinkAsync(
            db,
            purchase.Items,
            [.. lines.Select(l => l.ParentIndex)],
            ct
        );

        return new ReceiptIngestResult(
            purchase.Id,
            purchase.Status,
            lines.Count(l => l.Item.LineType == PurchaseLineType.Item) - unmatched,
            unmatched,
            WasKnown: false
        );
    }

    private static async Task<ReceiptIngestResult?> ReadKnownAsync(
        HomeBaseDbContext db,
        string externalId,
        CancellationToken ct
    )
    {
        var known = await db
            .Purchases.Where(p => p.ExternalId == externalId)
            .Select(p => new { p.Id, p.Status })
            .FirstOrDefaultAsync(ct);

        if (known is null)
        {
            return null;
        }

        var counts = await CountMatchesAsync(db, known.Id, ct);

        return new ReceiptIngestResult(
            known.Id,
            known.Status,
            counts.Matched,
            counts.Unmatched,
            WasKnown: true
        );
    }

    private static async Task<List<BuiltLine>> BuildLinesAsync(
        HomeBaseDbContext db,
        int? storeId,
        IReadOnlyList<ReceiptItem> incoming,
        CancellationToken ct
    )
    {
        var lines = new List<BuiltLine>(incoming.Count);
        var lastItemIndex = -1;

        for (var index = 0; index < incoming.Count; index++)
        {
            var source = incoming[index];
            var lineType = ResolveLineType(source, lastItemIndex >= 0);

            var item = new PurchaseItem
            {
                LineNo = source.LineNo ?? index + 1,
                LineType = lineType,
                RawText = source.RawText.TrimToNull(),
                Gtin = source.Gtin.TrimToNull(),
                Quantity = source.Quantity,
                UnitPrice = source.UnitPrice,
                LineTotal = source.LineTotal,
                Discount = source.Discount ?? 0m,
                TaxRate = source.TaxRate,
                IsPromo = source.IsPromo ?? AliasText.LooksPromotional(source.RawText),
            };

            var parentIndex = lineType == PurchaseLineType.Discount ? lastItemIndex : (int?)null;

            if (lineType == PurchaseLineType.Item)
            {
                var match = await ProductMatcher.MatchAsync(
                    db,
                    storeId,
                    item.Gtin,
                    item.RawText,
                    ct
                );

                item.ProductId = match.ProductId;
                item.MatchStatus = match.Status;
                item.MatchConfidence = match.Confidence;
                item.SuggestedProductId = match.SuggestedProductId;

                lastItemIndex = index;
            }

            if (parentIndex is { } parent)
            {
                lines[parent].Item.IsPromo = true;
            }

            lines.Add(new BuiltLine(item, source, parentIndex));
        }

        await NormalizeQuantitiesAsync(db, lines, ct);

        return lines;
    }

    internal static PurchaseLineType ResolveLineType(ReceiptItem source, bool hasItemAbove) =>
        source.LineType switch
        {
            { Length: > 0 } declared when Enum.TryParse<PurchaseLineType>(declared, true, out var parsed) =>
                parsed,
            _ when source.LineTotal < 0 && hasItemAbove => PurchaseLineType.Discount,
            _ when source.LineTotal < 0 => PurchaseLineType.DepositReturn,
            _ => PurchaseLineType.Item,
        };

    private static async Task NormalizeQuantitiesAsync(
        HomeBaseDbContext db,
        List<BuiltLine> lines,
        CancellationToken ct
    )
    {
        var productIds = lines
            .Where(l => l.Item.ProductId is not null)
            .Select(l => l.Item.ProductId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
        {
            return;
        }

        var products = await db
            .Products.Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.BaseUnit,
                p.PackageSize,
            })
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var line in lines)
        {
            if (
                line.Item.ProductId is { } productId
                && products.TryGetValue(productId, out var product)
            )
            {
                line.Item.QuantityBase = ToBaseQuantity(
                    line.Source.Quantity,
                    line.Source.Unit,
                    product.BaseUnit,
                    product.PackageSize
                );
            }
        }
    }

    // Weighed goods carry their unit; everything else counts packages.
    internal static decimal ToBaseQuantity(
        decimal quantity,
        string? unit,
        BaseUnit baseUnit,
        decimal packageSize
    ) => UnitConversion.ToBase(quantity, unit, baseUnit) ?? quantity * packageSize;

    private static async Task<(int Matched, int Unmatched)> CountMatchesAsync(
        HomeBaseDbContext db,
        long purchaseId,
        CancellationToken ct
    )
    {
        var lines = await db
            .PurchaseItems.ForPurchase(purchaseId)
            .ItemLines()
            .Select(i => i.ProductId)
            .ToListAsync(ct);

        var matched = lines.Count(id => id is not null);

        return (matched, lines.Count - matched);
    }

    private sealed record BuiltLine(PurchaseItem Item, ReceiptItem Source, int? ParentIndex)
    {
        public bool NeedsReview =>
            Item.LineType == PurchaseLineType.Item && Item.ProductId is null;
    }
}
