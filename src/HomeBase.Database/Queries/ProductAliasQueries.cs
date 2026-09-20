using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class ProductAliasQueries
{
    public static Task<ProductAlias?> ForTextAsync(
        this IQueryable<ProductAlias> aliases,
        int? storeId,
        string normalizedText,
        CancellationToken ct = default
    ) =>
        aliases.FirstOrDefaultAsync(
            a => a.StoreId == storeId && a.NormalizedText == normalizedText,
            ct
        );

    public static async Task<AliasScore?> BestMatchAsync(
        this IQueryable<ProductAlias> aliases,
        string normalizedText,
        CancellationToken ct = default
    )
    {
        var best = await aliases
            .Select(a => new
            {
                a.ProductId,
                Score = EF.Functions.TrigramsSimilarity(a.NormalizedText, normalizedText),
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync(ct);

        return best is null ? null : new AliasScore(best.ProductId, best.Score);
    }

#pragma warning disable CA1304, CA1311
    public static async Task<AliasScore?> BestMatchAsync(
        this IQueryable<Product> products,
        string normalizedText,
        CancellationToken ct = default
    )
    {
        var best = await products
            .Select(p => new
            {
                ProductId = p.Id,
                Score = EF.Functions.TrigramsSimilarity(p.Name.ToLower(), normalizedText),
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefaultAsync(ct);

        return best is null ? null : new AliasScore(best.ProductId, best.Score);
    }
#pragma warning restore CA1304, CA1311
}

public sealed record AliasScore(int ProductId, double Score);
