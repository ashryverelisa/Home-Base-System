using HomeBase.Database.Enums;

namespace HomeBase.Features.Shopping;

public interface IShoppingService
{
    Task<IReadOnlyList<ShoppingListRow>> GetListsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ShoppingItemRow>> GetItemsAsync(
        int listId,
        bool settled = false,
        CancellationToken ct = default
    );

    Task<ShoppingSaveResult> AddProductAsync(
        AddProductRequest request,
        CancellationToken ct = default
    );

    Task<ShoppingSaveResult> AddFreeTextAsync(
        AddFreeTextRequest request,
        CancellationToken ct = default
    );

    Task SetStatusAsync(long itemId, ShoppingListItemStatus status, CancellationToken ct = default);

    Task SetQuantityAsync(long itemId, decimal? quantity, CancellationToken ct = default);

    Task ToggleImportantAsync(long itemId, CancellationToken ct = default);

    Task RemoveAsync(long itemId, CancellationToken ct = default);

    Task<int> ClearSettledAsync(int listId, CancellationToken ct = default);

    Task<int> RunAutoRestockAsync(CancellationToken ct = default);
}
