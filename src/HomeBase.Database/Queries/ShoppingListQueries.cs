using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class ShoppingListQueries
{
    public static IQueryable<ShoppingList> Active(this IQueryable<ShoppingList> lists) =>
        lists.Where(l => !l.Archived);

    public static IOrderedQueryable<ShoppingList> InDisplayOrder(
        this IQueryable<ShoppingList> lists
    ) => lists.OrderBy(l => l.SortOrder).ThenBy(l => l.Name);

    public static IQueryable<ShoppingListItem> OnList(
        this IQueryable<ShoppingListItem> items,
        int listId
    ) => items.Where(i => i.ListId == listId);

    public static IQueryable<ShoppingListItem> Open(this IQueryable<ShoppingListItem> items) =>
        items.Where(i => i.Status == ShoppingListItemStatus.Open);

    public static IQueryable<ShoppingListItem> Settled(this IQueryable<ShoppingListItem> items) =>
        items.Where(i => i.Status != ShoppingListItemStatus.Open);

    public static IOrderedQueryable<ShoppingListItem> MostUrgentFirst(
        this IQueryable<ShoppingListItem> items
    ) => items.OrderByDescending(i => i.Priority).ThenBy(i => i.AddedAt);

    public static IOrderedQueryable<ShoppingListItem> NewestSettledFirst(
        this IQueryable<ShoppingListItem> items
    ) => items.OrderByDescending(i => i.BoughtAt ?? i.AddedAt);

    public static Task<ShoppingListItem?> OpenForProductAsync(
        this IQueryable<ShoppingListItem> items,
        int listId,
        int productId,
        CancellationToken ct = default
    ) =>
        items
            .OnList(listId)
            .Open()
            .FirstOrDefaultAsync(i => i.ProductId == productId, ct);

    public static Task<Dictionary<int, int>> OpenCountsByListAsync(
        this IQueryable<ShoppingListItem> items,
        CancellationToken ct = default
    ) =>
        items
            .Open()
            .GroupBy(i => i.ListId)
            .Select(g => new { ListId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ListId, x => x.Count, ct);

    public static Task<HashSet<int>> OpenProductIdsAsync(
        this IQueryable<ShoppingListItem> items,
        CancellationToken ct = default
    ) =>
        items
            .Open()
            .Where(i => i.ProductId != null)
            .Select(i => i.ProductId!.Value)
            .Distinct()
            .ToHashSetAsync(ct);
}
