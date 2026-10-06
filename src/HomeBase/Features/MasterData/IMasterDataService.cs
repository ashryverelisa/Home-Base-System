using HomeBase.Database.Entities;
using HomeBase.Features.Common;

namespace HomeBase.Features.MasterData;

public interface IMasterDataService
{
    Task<IReadOnlyList<LocationRow>> GetLocationsAsync(CancellationToken ct = default);

    Task<SaveResult> SaveLocationAsync(StorageLocation input, CancellationToken ct = default);

    Task<SaveResult> DeleteLocationAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryRow>> GetCategoriesAsync(CancellationToken ct = default);

    Task<SaveResult> SaveCategoryAsync(Category input, CancellationToken ct = default);

    Task<SaveResult> DeleteCategoryAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<StoreRow>> GetStoresAsync(CancellationToken ct = default);

    Task<SaveResult> SaveStoreAsync(Store input, CancellationToken ct = default);

    Task<SaveResult> DeleteStoreAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<ShoppingListAdminRow>> GetShoppingListsAsync(CancellationToken ct = default);

    Task<SaveResult> SaveShoppingListAsync(ShoppingList input, CancellationToken ct = default);

    Task<SaveResult> SetDefaultShoppingListAsync(int id, CancellationToken ct = default);

    Task<SaveResult> SetShoppingListArchivedAsync(
        int id,
        bool archived,
        CancellationToken ct = default
    );

    Task MoveShoppingListAsync(int id, int offset, CancellationToken ct = default);
}
