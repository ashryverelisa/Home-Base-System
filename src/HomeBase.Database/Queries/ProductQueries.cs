using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class ProductQueries
{
    public static IQueryable<Product> WithCategory(this IQueryable<Product> products) =>
        products.Include(p => p.Category);

    public static IQueryable<Product> MatchingSearch(this IQueryable<Product> products, string term) =>
        products.Where(p =>
            EF.Functions.ILike(p.Name, $"%{term}%")
            || (p.Brand != null && EF.Functions.ILike(p.Brand, $"%{term}%"))
            || (p.Gtin != null && p.Gtin == term));

    public static IQueryable<Product> WithMinimumStock(this IQueryable<Product> products) =>
        products.Where(p => p.MinStockBase != null && p.MinStockBase > 0);

    public static Task<Product?> ByGtinAsync(
        this IQueryable<Product> products,
        string gtin,
        CancellationToken ct = default) =>
        products.FirstOrDefaultAsync(p => p.Gtin == gtin, ct);

    public static Task<bool> GtinTakenAsync(
        this IQueryable<Product> products,
        string gtin,
        int exceptProductId,
        CancellationToken ct = default) =>
        products.AnyAsync(p => p.Gtin == gtin && p.Id != exceptProductId, ct);
}
