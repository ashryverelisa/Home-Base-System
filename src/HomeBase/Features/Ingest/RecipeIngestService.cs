using System.Globalization;
using System.Text.RegularExpressions;
using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
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

public sealed partial class RecipeIngestService(IDbContextFactory<HomeBaseDbContext> factory) : IRecipeIngestService
{
    [GeneratedRegex(
        @"^\s*(?<qty>\d+(?:[.,]\d+)?)?\s*(?<unit>kg|g|l|ml|el|tl|stk|stück|pcs|prise|packung)?\.?\s+(?<name>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex IngredientLine { get; }

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
            var parsed = Parse(incoming);

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
                ingredient.QuantityBase = await ToBaseQuantityAsync(
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

    internal static ParsedIngredient Parse(RecipeIngredientRequest incoming)
    {
        if (incoming.Name is { Length: > 0 } name)
        {
            return new ParsedIngredient(name.Trim(), incoming.Quantity, incoming.Unit?.Trim());
        }

        if (incoming.Text is not { Length: > 0 } text)
        {
            return new ParsedIngredient(string.Empty, null, null);
        }

        var match = IngredientLine.Match(text);

        if (!match.Success)
        {
            return new ParsedIngredient(text.Trim(), incoming.Quantity, incoming.Unit?.Trim());
        }

        var quantity = incoming.Quantity;

        if (
            quantity is null
            && match.Groups["qty"].Success
            && decimal.TryParse(
                match.Groups["qty"].Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedQuantity
            )
        )
        {
            quantity = parsedQuantity;
        }

        var unit = match.Groups["unit"].Success
            ? match.Groups["unit"].Value
            : incoming.Unit?.Trim();

        return new ParsedIngredient(match.Groups["name"].Value.Trim(), quantity, unit);
    }

    internal static async Task<decimal?> ToBaseQuantityAsync(
        HomeBaseDbContext db,
        int productId,
        decimal? quantity,
        string? unit,
        CancellationToken ct
    )
    {
        if (quantity is not { } value || value <= 0)
        {
            return null;
        }

        var product = await db
            .Products.Where(p => p.Id == productId)
            .Select(p => new { p.BaseUnit, p.PieceWeightBase })
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return null;
        }

        return (unit?.ToLowerInvariant(), product.BaseUnit) switch
        {
            ("kg", BaseUnit.Gram) or ("l", BaseUnit.Milliliter) => value * 1000m,
            ("g", BaseUnit.Gram) or ("ml", BaseUnit.Milliliter) => value,
            (_, BaseUnit.Piece) when IsCount(unit) => value,
            (_, BaseUnit.Gram) when IsCount(unit) => product.PieceWeightBase is { } weight
                ? value * weight
                : null,
            _ => null,
        };
    }

    private static bool IsCount(string? unit) =>
        unit is null or "" or "stk" or "stück" or "pcs" or "packung";

    internal sealed record ParsedIngredient(string Name, decimal? Quantity, string? Unit);
}
