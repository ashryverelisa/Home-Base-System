using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Ingest;

public sealed record ReceiptIngestResult(
    long PurchaseId,
    PurchaseStatus Status,
    int Matched,
    int Unmatched,
    bool WasKnown
);

public sealed class ReceiptIngestService(IDbContextFactory<HomeBaseDbContext> factory)
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

        if (request.ExternalId is { Length: > 0 } externalId)
        {
            var known = await db
                .Purchases.Where(p => p.ExternalId == externalId)
                .Select(p => new { p.Id, p.Status })
                .FirstOrDefaultAsync(ct);

            if (known is not null)
            {
                var counts = await CountMatchesAsync(db, known.Id, ct);

                return new ReceiptIngestResult(
                    known.Id,
                    known.Status,
                    counts.Matched,
                    counts.Unmatched,
                    WasKnown: true
                );
            }
        }

        var storeId = await ResolveStoreAsync(db, request.Store, ct);

        var purchase = new Purchase
        {
            StoreId = storeId,
            PurchasedAt = request.PurchasedAt ?? DateTimeOffset.UtcNow,
            Total = request.Total,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim(),
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
            return await ReadKnownAsync(request.ExternalId, ct);
        }

        await LinkDiscountsAsync(db, purchase, lines, ct);

        return new ReceiptIngestResult(
            purchase.Id,
            purchase.Status,
            lines.Count(l => l.Item.LineType == PurchaseLineType.Item) - unmatched,
            unmatched,
            WasKnown: false
        );
    }

    private async Task<ReceiptIngestResult?> ReadKnownAsync(
        string externalId,
        CancellationToken ct
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

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
                RawText = string.IsNullOrWhiteSpace(source.RawText) ? null : source.RawText.Trim(),
                Gtin = string.IsNullOrWhiteSpace(source.Gtin) ? null : source.Gtin.Trim(),
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

                if (match.ProductId is { } productId)
                {
                    item.QuantityBase = await NormalizeQuantityAsync(
                        db,
                        productId,
                        source.Quantity,
                        source.Unit,
                        ct
                    );
                }

                lastItemIndex = index;
            }

            if (parentIndex is { } parent)
            {
                lines[parent].Item.IsPromo = true;
            }

            lines.Add(new BuiltLine(item, parentIndex));
        }

        return lines;
    }

    private static PurchaseLineType ResolveLineType(ReceiptItem source, bool hasItemAbove) =>
        source.LineType switch
        {
            { Length: > 0 } declared when Enum.TryParse<PurchaseLineType>(declared, true, out var parsed) =>
                parsed,
            _ when source.LineTotal < 0 && hasItemAbove => PurchaseLineType.Discount,
            _ when source.LineTotal < 0 => PurchaseLineType.DepositReturn,
            _ => PurchaseLineType.Item,
        };

    private static async Task<decimal?> NormalizeQuantityAsync(
        HomeBaseDbContext db,
        int productId,
        decimal quantity,
        string? unit,
        CancellationToken ct
    )
    {
        var product = await db
            .Products.Where(p => p.Id == productId)
            .Select(p => new { p.BaseUnit, p.PackageSize })
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return null;
        }

        return (unit?.Trim().ToLowerInvariant(), product.BaseUnit) switch
        {
            ("kg", BaseUnit.Gram) or ("l", BaseUnit.Milliliter) => quantity * 1000m,
            ("g", BaseUnit.Gram) or ("ml", BaseUnit.Milliliter) => quantity,
            _ => quantity * product.PackageSize,
        };
    }

    private static async Task<int?> ResolveStoreAsync(
        HomeBaseDbContext db,
        ReceiptStore? store,
        CancellationToken ct
    )
    {
        if (store is null)
        {
            return null;
        }

        if (store.TaxId is { Length: > 0 } taxId)
        {
            var byTaxId = await db.Stores.FirstOrDefaultAsync(s => s.TaxId == taxId, ct);

            if (byTaxId is not null)
            {
                return byTaxId.Id;
            }
        }

        if (string.IsNullOrWhiteSpace(store.Name))
        {
            return null;
        }

        var name = store.Name.Trim();
        var existing = await db.Stores.ByNameAsync(name, ct);

        if (existing is not null)
        {
            return existing.Id;
        }

        var created = new Store { Name = name, TaxId = store.TaxId };

        db.Stores.Add(created);
        await db.SaveChangesAsync(ct);

        return created.Id;
    }

    private static async Task LinkDiscountsAsync(
        HomeBaseDbContext db,
        Purchase purchase,
        List<BuiltLine> lines,
        CancellationToken ct
    )
    {
        var linked = false;

        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].ParentIndex is not { } parent)
            {
                continue;
            }

            purchase.Items[index].ParentItemId = purchase.Items[parent].Id;
            linked = true;
        }

        if (linked)
        {
            await db.SaveChangesAsync(ct);
        }
    }

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

    private sealed record BuiltLine(PurchaseItem Item, int? ParentIndex)
    {
        public bool NeedsReview =>
            Item.LineType == PurchaseLineType.Item && Item.ProductId is null;
    }
}
