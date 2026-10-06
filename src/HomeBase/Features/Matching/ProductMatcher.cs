using HomeBase.Database;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;

namespace HomeBase.Features.Matching;

public sealed record ProductMatch(
    int? ProductId,
    MatchStatus Status,
    float? Confidence,
    int? SuggestedProductId
)
{
    public static readonly ProductMatch None = new(null, MatchStatus.Unmatched, null, null);

    public bool IsMatched => ProductId is not null;
}

public static class ProductMatcher
{
    public const double AutoThreshold = 0.75;
    public const double SuggestThreshold = 0.45;

    public static async Task<ProductMatch> MatchAsync(
        HomeBaseDbContext db,
        int? storeId,
        string? gtin,
        string? rawText,
        CancellationToken ct = default
    )
    {
        if (!string.IsNullOrWhiteSpace(gtin))
        {
            var byGtin = await db.Products.ByGtinAsync(gtin.Trim(), ct);

            if (byGtin is not null)
            {
                return new ProductMatch(byGtin.Id, MatchStatus.Gtin, 1f, null);
            }
        }

        var normalized = AliasText.Normalize(rawText);

        if (normalized.Length == 0)
        {
            return ProductMatch.None;
        }

        if (storeId is { } id)
        {
            var storeAlias = await db.ProductAliases.ForTextAsync(id, normalized, ct);

            if (storeAlias is not null)
            {
                return new ProductMatch(storeAlias.ProductId, MatchStatus.Alias, 1f, null);
            }
        }

        var globalAlias = await db.ProductAliases.ForTextAsync(null, normalized, ct);

        if (globalAlias is not null)
        {
            return new ProductMatch(globalAlias.ProductId, MatchStatus.Alias, 0.9f, null);
        }

        var best = await BestScoreAsync(db, normalized, ct);

        return best switch
        {
            { Score: >= AutoThreshold } => new ProductMatch(
                best.ProductId,
                MatchStatus.Fuzzy,
                (float)best.Score,
                null
            ),
            { Score: >= SuggestThreshold } => new ProductMatch(
                null,
                MatchStatus.Unmatched,
                (float)best.Score,
                best.ProductId
            ),
            _ => ProductMatch.None,
        };
    }

    private static async Task<AliasScore?> BestScoreAsync(
        HomeBaseDbContext db,
        string normalized,
        CancellationToken ct
    )
    {
        var byAlias = await db.ProductAliases.BestMatchAsync(normalized, ct);
        var byName = await db.Products.BestMatchAsync(normalized, ct);

        return byAlias is null || (byName is not null && byName.Score > byAlias.Score)
            ? byName
            : byAlias;
    }
}
