using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Queries;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.Catalog;

public sealed class CatalogService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IStringLocalizer<AppStrings> localizer
) : ICatalogService
{
    public async Task<IReadOnlyList<ProductRow>> SearchAsync(
        string? term = null,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var products = string.IsNullOrWhiteSpace(term)
            ? db.Products
            : db.Products.MatchingSearch(term.Trim());

        var rows = await products
            .OrderBy(p => p.Name)
            .Select(ProductRow.Projection)
            .ToListAsync(ct);

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return [.. rows.Select(r => r with { StockBase = totals.GetValueOrDefault(r.Id) })];
    }

    public async Task<Product?> FindAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Products.FindAsync([id], ct);
    }

    public async Task<Product?> FindByGtinAsync(string gtin, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Products.ByGtinAsync(gtin.Trim(), ct);
    }

    public async Task<ProductSaveResult> SaveAsync(Product product, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return ProductSaveResult.Failed(localizer["Catalog.NameRequired"]);
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        product.Gtin = string.IsNullOrWhiteSpace(product.Gtin) ? null : product.Gtin.Trim();

        if (product.Gtin is { } gtin && await db.Products.GtinTakenAsync(gtin, product.Id, ct))
        {
            return ProductSaveResult.Failed(localizer["Catalog.GtinTaken", gtin]);
        }

        if (product.Id == 0)
        {
            db.Products.Add(product);
        }
        else
        {
            db.Products.Update(product);
        }

        await db.SaveChangesAsync(ct);

        return ProductSaveResult.Ok(product.Id);
    }

    public async Task LearnShelfLifeAsync(int productId, int days, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db
            .Products.Where(p => p.Id == productId && p.DefaultShelfLifeDays == null)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DefaultShelfLifeDays, days), ct);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Categories.OrderBy(c => c.Kind).ThenBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StorageLocation>> GetLocationsAsync(
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return LocationTree.Flatten(await db.StorageLocations.WithParent().ToListAsync(ct));
    }
}
