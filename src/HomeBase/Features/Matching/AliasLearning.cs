using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;

namespace HomeBase.Features.Matching;

// A confirmed assignment becomes an alias, so the same text matches on its own next time.
// Receipt lines and recipe ingredients share it: one alias helps both.
public static class AliasLearning
{
    public static async Task LearnAsync(
        HomeBaseDbContext db,
        string? rawText,
        int productId,
        int? storeId,
        CancellationToken ct = default
    )
    {
        var normalized = AliasText.Normalize(rawText);

        if (normalized.Length == 0)
        {
            return;
        }

        var existing = await db.ProductAliases.ForTextAsync(storeId, normalized, ct);

        if (existing is not null)
        {
            existing.ProductId = productId;
            existing.TimesSeen++;

            return;
        }

        db.ProductAliases.Add(
            new ProductAlias
            {
                ProductId = productId,
                StoreId = storeId,
                RawText = rawText ?? normalized,
                NormalizedText = normalized,
                Source = AliasSource.Learned,
            }
        );
    }
}
