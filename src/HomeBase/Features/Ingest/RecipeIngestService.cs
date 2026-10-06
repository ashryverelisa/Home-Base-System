using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Features.Matching;
using HomeBase.Features.Recipes;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Ingest;

public sealed record RecipeRequest(
    string? Name,
    int? Servings,
    int? PrepMinutes,
    int? CookMinutes,
    string? Instructions,
    string? SourceUrl,
    string? ImageUrl,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<RecipeIngredientRequest>? Ingredients
);

public sealed record RecipeIngredientRequest(
    string? Text,
    decimal? Quantity,
    string? Unit,
    string? Name,
    bool? IsOptional,
    string? Note
);

public sealed record RecipeIngestResult(int RecipeId, int Matched, int Unmatched, bool WasKnown);

public sealed class RecipeIngestService(IDbContextFactory<HomeBaseDbContext> factory) : IRecipeIngestService
{
    public async Task<RecipeIngestResult?> IngestAsync(
        RecipeRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return null;
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        if (request.SourceUrl is { Length: > 0 } sourceUrl)
        {
            var known = await db
                .Recipes.Where(r => r.SourceUrl == sourceUrl)
                .Select(r => new
                {
                    r.Id,
                    Matched = r.Ingredients.Count(i => i.ProductId != null),
                    Total = r.Ingredients.Count,
                })
                .FirstOrDefaultAsync(ct);

            if (known is not null)
            {
                return new RecipeIngestResult(
                    known.Id,
                    known.Matched,
                    known.Total - known.Matched,
                    WasKnown: true
                );
            }
        }

        var recipe = new Recipe
        {
            Name = request.Name.Trim(),
            Servings = request.Servings is > 0 ? request.Servings.Value : 2,
            PrepMinutes = request.PrepMinutes,
            CookMinutes = request.CookMinutes,
            Instructions = request.Instructions,
            SourceUrl = request.SourceUrl,
            ImageUrl = request.ImageUrl,
            Tags = [.. request.Tags ?? []],
        };

        var matched = 0;
        var order = 0;

        foreach (var incoming in request.Ingredients ?? [])
        {
            var parsed = IngredientParser.Parse(
                incoming.Text,
                incoming.Quantity,
                incoming.Unit,
                incoming.Name
            );

            if (parsed.Name.Length == 0)
            {
                continue;
            }

            var match = await ProductMatcher.MatchAsync(db, null, null, parsed.Name, ct);

            var ingredient = new RecipeIngredient
            {
                SortOrder = order++,
                IsOptional = incoming.IsOptional ?? false,
                Note = incoming.Note,
            };

            if (match.ProductId is { } productId)
            {
                ingredient.ProductId = productId;
                ingredient.QuantityBase = await IngredientParser.ToBaseQuantityAsync(
                    db,
                    productId,
                    parsed.Quantity,
                    parsed.Unit,
                    ct
                );

                matched++;
            }
            else
            {
                ingredient.FreeText = incoming.Text?.Trim() ?? parsed.Name;
                ingredient.NeedsReview = true;
            }

            recipe.Ingredients.Add(ingredient);
        }

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);

        return new RecipeIngestResult(
            recipe.Id,
            matched,
            recipe.Ingredients.Count - matched,
            WasKnown: false
        );
    }
}
