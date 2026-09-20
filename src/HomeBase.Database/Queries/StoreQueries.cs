using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class StoreQueries
{
    public static IOrderedQueryable<Store> InDisplayOrder(this IQueryable<Store> stores) =>
        stores.OrderBy(s => s.Name);

    public static Task<Store?> ByNameAsync(
        this IQueryable<Store> stores,
        string name,
        CancellationToken ct = default
    ) => stores.FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, name), ct);
}
