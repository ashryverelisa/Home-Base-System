using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using HomeBase.Features.Inventory;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.Purchases;

public sealed class PurchaseService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IStringLocalizer<AppStrings> localizer,
    TimeProvider time
) : IPurchaseService
{
    public async Task<IReadOnlyList<PurchaseRow>> GetPurchasesAsync(
        int take = 50,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .Purchases.NewestFirst()
            .Take(take)
            .Select(PurchaseRow.Projection)
            .ToListAsync(ct);

        return await WithLineCountsAsync(db, rows, ct);
    }

    public async Task<IReadOnlyList<PurchaseRow>> GetPendingAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .Purchases.Pending()
            .NewestFirst()
            .Select(PurchaseRow.Projection)
            .ToListAsync(ct);

        return await WithLineCountsAsync(db, rows, ct);
    }

    public async Task<PurchaseRow?> FindAsync(long id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .Purchases.Where(p => p.Id == id)
            .Select(PurchaseRow.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PurchaseLineRow>> GetLinesAsync(
        long purchaseId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .PurchaseItems.ForPurchase(purchaseId)
            .InReceiptOrder()
            .Select(PurchaseLineRow.Projection)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Store>> GetStoresAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Stores.InDisplayOrder().ToListAsync(ct);
    }

    public async Task<SaveResult<long>> SaveAsync(
        PurchaseDraft draft,
        CancellationToken ct = default
    )
    {
        if (draft.Lines.Count == 0)
        {
            return SaveResult.Failed<long>(localizer["Purchases.NoLines"]);
        }

        if (draft.Lines.Any(l => l.IsItem && l.ProductId is null))
        {
            return SaveResult.Failed<long>(localizer["Purchases.ProductRequired"]);
        }

        var sum = draft.LineSum;

        if (draft.ReceiptTotal is { } receiptTotal && receiptTotal != sum)
        {
            return SaveResult.Failed<long>(
                localizer["Purchases.TotalMismatch", Units.Money(sum), Units.Money(receiptTotal)]
            );
        }

        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var purchase = new Purchase
        {
            StoreId =
                draft.StoreId
                ?? await StoreResolver.FindOrCreateAsync(db, draft.StoreName, taxId: null, ct),
            PurchasedAt = draft.PurchasedAt,
            Total = sum,
            PaymentMethod = draft.PaymentMethod.TrimToNull(),
            Source = PurchaseSource.Manual,
            Status = PurchaseStatus.Confirmed,
        };

        for (var index = 0; index < draft.Lines.Count; index++)
        {
            purchase.Items.Add(ToItem(draft.Lines[index], index + 1));
        }

        db.Purchases.Add(purchase);
        await db.SaveChangesAsync(ct);

        await DiscountLinker.LinkAsync(
            db,
            purchase.Items,
            [
                .. draft.Lines.Select(line =>
                    line is { LineType: PurchaseLineType.Discount, ParentIndex: { } parent }
                        ? parent
                        : (int?)null
                ),
            ],
            ct
        );

        if (draft.BookIntoStock)
        {
            var bestBefore = draft
                .Lines.Select((line, index) => (purchase.Items[index].Id, line.BestBefore))
                .ToDictionary(x => x.Id, x => x.BestBefore);

            await BookPurchaseAsync(db, purchase.Id, bestBefore, ct);
        }

        await transaction.CommitAsync(ct);

        return SaveResult.Ok(purchase.Id);
    }

    public async Task<bool> ConfirmAsync(long purchaseId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var purchase = await db.Purchases.FindAsync([purchaseId], ct);

        if (purchase is null || purchase.Status == PurchaseStatus.Confirmed)
        {
            return false;
        }

        purchase.Status = PurchaseStatus.Confirmed;
        await db.SaveChangesAsync(ct);

        await BookPurchaseAsync(db, purchaseId, bestBefore: null, ct);

        await transaction.CommitAsync(ct);

        return true;
    }

    internal static PurchaseItem ToItem(PurchaseDraftLine line, int lineNo)
    {
        var total = line.SignedTotal;
        var quantity = line.IsItem ? line.Quantity : 1m;

        return new PurchaseItem
        {
            LineNo = lineNo,
            LineType = line.LineType,
            ProductId = line.IsItem ? line.ProductId : null,
            RawText = line.RawText.TrimToNull(),
            Quantity = quantity,
            QuantityBase = line.IsItem ? line.QuantityBase : null,
            UnitPrice = quantity == 0 ? null : total / quantity,
            LineTotal = total,
            IsPromo = line.IsPromo,
            MatchStatus =
                line.IsItem && line.ProductId is not null
                    ? MatchStatus.Manual
                    : MatchStatus.Unmatched,
        };
    }

    private static async Task<IReadOnlyList<PurchaseRow>> WithLineCountsAsync(
        HomeBaseDbContext db,
        List<PurchaseRow> rows,
        CancellationToken ct
    )
    {
        if (rows.Count == 0)
        {
            return rows;
        }

        var counts = await db.PurchaseItems.LineCountsAsync([.. rows.Select(r => r.Id)], ct);

        return
        [
            .. rows.Select(r =>
                counts.TryGetValue(r.Id, out var count)
                    ? r with { LineCount = count.Lines, UnmatchedCount = count.Unmatched }
                    : r
            ),
        ];
    }

    private async Task BookPurchaseAsync(
        HomeBaseDbContext db,
        long purchaseId,
        IReadOnlyDictionary<long, DateOnly?>? bestBefore,
        CancellationToken ct
    )
    {
        var lines = await db
            .PurchaseItems.ForPurchase(purchaseId)
            .ItemLines()
            .Where(i => i.ProductId != null && i.QuantityBase > 0)
            .Select(i => new BookableLine(
                i.Id,
                i.ProductId!.Value,
                i.QuantityBase!.Value,
                i.Product!.DefaultLocationId,
                i.Product!.DefaultShelfLifeDays
            ))
            .ToListAsync(ct);

        if (lines.Count == 0)
        {
            return;
        }

        var today = time.Today();

        foreach (var line in lines)
        {
            StockLedger.AddLot(
                db,
                new BookInRequest(
                    line.ProductId,
                    line.QuantityBase,
                    line.DefaultLocationId,
                    ShelfLife.Resolve(
                        bestBefore?.GetValueOrDefault(line.Id),
                        line.DefaultShelfLifeDays,
                        today
                    ),
                    line.Id
                )
            );
        }

        await db.SaveChangesAsync(ct);

        foreach (var line in lines)
        {
            await SettleShoppingItemsAsync(db, line.ProductId, line.Id, ct);
        }
    }

    private async Task SettleShoppingItemsAsync(
        HomeBaseDbContext db,
        int productId,
        long purchaseItemId,
        CancellationToken ct
    )
    {
        var boughtAt = time.GetUtcNow();

        await db
            .ShoppingListItems.Open()
            .Where(i => i.ProductId == productId)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(i => i.Status, ShoppingListItemStatus.Bought)
                        .SetProperty(i => i.BoughtAt, boughtAt)
                        .SetProperty(i => i.PurchaseItemId, purchaseItemId),
                ct
            );
    }

    private sealed record BookableLine(
        long Id,
        int ProductId,
        decimal QuantityBase,
        int? DefaultLocationId,
        int? DefaultShelfLifeDays
    );
}
