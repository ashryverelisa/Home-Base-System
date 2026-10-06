using HomeBase.Database.Entities;
using HomeBase.Features.Common;

namespace HomeBase.Features.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<ProductRow>> SearchAsync(string? term = null, CancellationToken ct = default);

    Task<Product?> FindAsync(int id, CancellationToken ct = default);

    Task<Product?> FindByGtinAsync(string gtin, CancellationToken ct = default);

    Task<ProductRow?> FindRowByGtinAsync(string gtin, CancellationToken ct = default);

    Task<SaveResult<int>> SaveAsync(Product product, CancellationToken ct = default);

    Task LearnShelfLifeAsync(int productId, int days, CancellationToken ct = default);

    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<StorageLocation>> GetLocationsAsync(CancellationToken ct = default);
}
