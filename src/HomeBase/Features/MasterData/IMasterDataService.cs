using HomeBase.Database.Entities;

namespace HomeBase.Features.MasterData;

public interface IMasterDataService
{
    Task<IReadOnlyList<LocationRow>> GetLocationsAsync(CancellationToken ct = default);

    Task<MasterDataResult> SaveLocationAsync(StorageLocation input, CancellationToken ct = default);

    Task<MasterDataResult> DeleteLocationAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<CategoryRow>> GetCategoriesAsync(CancellationToken ct = default);

    Task<MasterDataResult> SaveCategoryAsync(Category input, CancellationToken ct = default);

    Task<MasterDataResult> DeleteCategoryAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<StoreRow>> GetStoresAsync(CancellationToken ct = default);

    Task<MasterDataResult> SaveStoreAsync(Store input, CancellationToken ct = default);

    Task<MasterDataResult> DeleteStoreAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<ShoppingListAdminRow>> GetShoppingListsAsync(CancellationToken ct = default);

    Task<MasterDataResult> SaveShoppingListAsync(ShoppingList input, CancellationToken ct = default);

    Task<MasterDataResult> SetDefaultShoppingListAsync(int id, CancellationToken ct = default);

    Task<MasterDataResult> SetShoppingListArchivedAsync(
        int id,
        bool archived,
        CancellationToken ct = default
    );

    Task MoveShoppingListAsync(int id, int offset, CancellationToken ct = default);
}
