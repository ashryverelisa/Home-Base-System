using HomeBase.Database.Enums;
using HomeBase.Features.Common;

namespace HomeBase.Features.Shopping;

public interface IShoppingService
{
    Task<IReadOnlyList<ShoppingListRow>> GetListsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ShoppingItemRow>> GetItemsAsync(
        int listId,
        bool settled = false,
        CancellationToken ct = default
    );

    Task<SaveResult<long>> AddProductAsync(
        AddProductRequest request,
        CancellationToken ct = default
    );

    Task<SaveResult<long>> AddFreeTextAsync(
        AddFreeTextRequest request,
        CancellationToken ct = default
    );

    Task SetStatusAsync(long itemId, ShoppingListItemStatus status, CancellationToken ct = default);

    Task SetQuantityAsync(long itemId, decimal? quantity, CancellationToken ct = default);

    Task SetTargetPriceAsync(long itemId, decimal? price, CancellationToken ct = default);

    Task<SaveResult> SetLinkAsync(long itemId, string? link, CancellationToken ct = default);

    Task SetPriorityAsync(long itemId, ShoppingPriority priority, CancellationToken ct = default);

    Task ToggleImportantAsync(long itemId, CancellationToken ct = default);

    Task RemoveAsync(long itemId, CancellationToken ct = default);

    Task<int> ClearSettledAsync(int listId, CancellationToken ct = default);

    Task<int> RunAutoRestockAsync(CancellationToken ct = default);
}
