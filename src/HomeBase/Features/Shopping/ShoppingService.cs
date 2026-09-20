using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.Shopping;

public sealed class ShoppingService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IStringLocalizer<AppStrings> localizer
)
{
    public async Task<IReadOnlyList<ShoppingListRow>> GetListsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .ShoppingLists.Active()
            .InDisplayOrder()
            .Select(ShoppingListRow.Projection)
            .ToListAsync(ct);

        var counts = await db.ShoppingListItems.OpenCountsByListAsync(ct);

        return [.. rows.Select(r => r with { OpenCount = counts.GetValueOrDefault(r.Id) })];
    }

    public async Task<IReadOnlyList<ShoppingItemRow>> GetItemsAsync(
        int listId,
        bool settled = false,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var items = db.ShoppingListItems.OnList(listId);

        var rows = await (
            settled ? items.Settled().NewestSettledFirst() : items.Open().MostUrgentFirst()
        )
            .Select(ShoppingItemRow.Projection)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return rows;
        }

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return
        [
            .. rows.Select(r =>
                r.ProductId is { } id ? r with { StockBase = totals.GetValueOrDefault(id) } : r
            ),
        ];
    }

    public async Task<ShoppingSaveResult> AddProductAsync(
        AddProductRequest request,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var existing = await db.ShoppingListItems.OpenForProductAsync(
            request.ListId,
            request.ProductId,
            ct
        );

        if (existing is not null)
        {
            if (request.Quantity is { } added)
            {
                existing.Quantity = (existing.Quantity ?? 0) + added;
            }

            await db.SaveChangesAsync(ct);

            return ShoppingSaveResult.Ok(existing.Id);
        }

        var item = new ShoppingListItem
        {
            ListId = request.ListId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Note = request.Note,
            AddedBy = request.Origin,
        };

        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);

        return ShoppingSaveResult.Ok(item.Id);
    }

    public async Task<ShoppingSaveResult> AddFreeTextAsync(
        AddFreeTextRequest request,
        CancellationToken ct = default
    )
    {
        var text = request.Text.Trim();

        if (text.Length == 0)
        {
            return ShoppingSaveResult.Failed(localizer["Shopping.TextRequired"]);
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        var item = new ShoppingListItem
        {
            ListId = request.ListId,
            FreeText = text,
            Quantity = request.Quantity,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim(),
            TargetPrice = request.TargetPrice,
            Note = request.Note,
            AddedBy = request.Origin,
        };

        db.ShoppingListItems.Add(item);
        await db.SaveChangesAsync(ct);

        return ShoppingSaveResult.Ok(item.Id);
    }

    public async Task SetStatusAsync(
        long itemId,
        ShoppingListItemStatus status,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var item = await db.ShoppingListItems.FindAsync([itemId], ct);

        if (item is null || item.Status == status)
        {
            return;
        }

        item.Status = status;
        item.BoughtAt = status == ShoppingListItemStatus.Bought ? DateTimeOffset.UtcNow : null;

        await db.SaveChangesAsync(ct);
    }

    public async Task SetQuantityAsync(
        long itemId,
        decimal? quantity,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var item = await db.ShoppingListItems.FindAsync([itemId], ct);

        if (item is null)
        {
            return;
        }

        item.Quantity = quantity is > 0 ? quantity : null;

        await db.SaveChangesAsync(ct);
    }

    public async Task ToggleImportantAsync(long itemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var item = await db.ShoppingListItems.FindAsync([itemId], ct);

        if (item is null)
        {
            return;
        }

        item.Priority = item.Priority > 0 ? 0 : 1;

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(long itemId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.ShoppingListItems.Where(i => i.Id == itemId).ExecuteDeleteAsync(ct);
    }

    public async Task<int> ClearSettledAsync(int listId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.ShoppingListItems.OnList(listId).Settled().ExecuteDeleteAsync(ct);
    }

    public async Task<int> RunAutoRestockAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lists = await db.ShoppingLists.Active().InDisplayOrder().ToListAsync(ct);

        if (lists.Count == 0)
        {
            return 0;
        }

        var candidates = await db
            .Products.WithMinimumStock()
            .Select(p => new RestockCandidate(
                p.Id,
                p.MinStockBase!.Value,
                p.TargetStockBase,
                p.PackageSize,
                p.IsFood,
                (CategoryKind?)p.Category!.Kind
            ))
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return 0;
        }

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);
        var listed = await db.ShoppingListItems.OpenProductIdsAsync(ct);

        var added = 0;

        foreach (var candidate in candidates)
        {
            if (listed.Contains(candidate.ProductId))
            {
                continue;
            }

            var stock = totals.GetValueOrDefault(candidate.ProductId);

            if (stock >= candidate.MinStockBase)
            {
                continue;
            }

            var target = candidate.TargetStockBase ?? candidate.MinStockBase;

            db.ShoppingListItems.Add(
                new ShoppingListItem
                {
                    ListId = TargetList(lists, candidate).Id,
                    ProductId = candidate.ProductId,
                    Quantity = WholePackages(target - stock, candidate.PackageSize),
                    AddedBy = ShoppingListItemOrigin.AutoRestock,
                }
            );

            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return added;
    }

    private static ShoppingList TargetList(
        List<ShoppingList> lists,
        RestockCandidate candidate
    )
    {
        var kind = candidate.IsFood
            ? ShoppingListKind.Groceries
            : candidate.CategoryKind switch
            {
                CategoryKind.Food => ShoppingListKind.Groceries,
                CategoryKind.Tech => ShoppingListKind.Tech,
                CategoryKind.Household => ShoppingListKind.Household,
                _ => (ShoppingListKind?)null,
            };

        return (kind is { } wanted ? lists.FirstOrDefault(l => l.Kind == wanted) : null)
            ?? lists.FirstOrDefault(l => l.IsDefault)
            ?? lists[0];
    }

    private static decimal WholePackages(decimal missing, decimal packageSize) =>
        packageSize > 0 ? Math.Ceiling(missing / packageSize) * packageSize : missing;

    private sealed record RestockCandidate(
        int ProductId,
        decimal MinStockBase,
        decimal? TargetStockBase,
        decimal PackageSize,
        bool IsFood,
        CategoryKind? CategoryKind
    );
}
