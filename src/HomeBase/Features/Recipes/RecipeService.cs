using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Queries;
using HomeBase.Features.Catalog;
using HomeBase.Features.Ingest;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.Recipes;

public sealed class RecipeService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IStringLocalizer<AppStrings> localizer
) : IRecipeService
{
    public async Task<IReadOnlyList<RecipeRow>> SearchAsync(
        string? term = null,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var recipes = string.IsNullOrWhiteSpace(term)
            ? db.Recipes
            : db.Recipes.MatchingSearch(term.Trim());

        return await recipes.InDisplayOrder().Select(RecipeRow.Projection).ToListAsync(ct);
    }

    public async Task<Recipe?> FindAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Recipes.WithIngredients().FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<IngredientLine>> GetIngredientsAsync(
        int recipeId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .RecipeIngredients.ForRecipe(recipeId)
            .InRecipeOrder()
            .Select(i => new IngredientLine(
                i.Id,
                i.ProductId,
                i.Product!.Name,
                i.FreeText,
                i.Product!.BaseUnit,
                i.QuantityBase,
                i.IsOptional,
                i.Note,
                i.NeedsReview
            ))
            .ToListAsync(ct);
    }

    public async Task<RecipeSaveResult> SaveAsync(Recipe recipe, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            return RecipeSaveResult.Failed(localizer["Recipes.NameRequired"]);
        }

        if (recipe.Servings <= 0)
        {
            return RecipeSaveResult.Failed(localizer["Recipes.ServingsRequired"]);
        }

        recipe.Name = recipe.Name.Trim();

        await using var db = await factory.CreateDbContextAsync(ct);

        if (recipe.Id == 0)
        {
            db.Recipes.Add(recipe);
        }
        else
        {
            db.Recipes.Update(recipe);
        }

        await db.SaveChangesAsync(ct);

        return RecipeSaveResult.Ok(recipe.Id);
    }

    public async Task DeleteAsync(int recipeId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.Recipes.Where(r => r.Id == recipeId).ExecuteDeleteAsync(ct);
    }

    public async Task<int> AddIngredientAsync(
        int recipeId,
        int? productId,
        string? freeText,
        decimal? quantityBase,
        bool isOptional = false,
        string? note = null,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var nextOrder = await db
            .RecipeIngredients.ForRecipe(recipeId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(ct);

        var ingredient = new RecipeIngredient
        {
            RecipeId = recipeId,
            ProductId = productId,
            FreeText = string.IsNullOrWhiteSpace(freeText) ? null : freeText.Trim(),
            QuantityBase = quantityBase,
            IsOptional = isOptional,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            SortOrder = (nextOrder ?? 0) + 1,
        };

        db.RecipeIngredients.Add(ingredient);
        await db.SaveChangesAsync(ct);

        return ingredient.Id;
    }

    public async Task RemoveIngredientAsync(int ingredientId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.RecipeIngredients.Where(i => i.Id == ingredientId).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<IngredientReview>> GetReviewAsync(
        int recipeId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var open = await db
            .RecipeIngredients.ForRecipe(recipeId)
            .Where(i => i.NeedsReview)
            .InRecipeOrder()
            .Select(i => new { i.Id, i.FreeText })
            .ToListAsync(ct);

        var reviews = new List<(int Id, string Text, string Name, int? SuggestedId)>();

        foreach (var ingredient in open)
        {
            var text = ingredient.FreeText ?? string.Empty;
            var name = ParseName(text);

            // Re-run the matcher: aliases learned since the import may already know this line.
            var match = await ProductMatcher.MatchAsync(db, null, null, name, ct);

            reviews.Add((ingredient.Id, text, name, match.ProductId ?? match.SuggestedProductId));
        }

        var suggestedIds = reviews.Where(r => r.SuggestedId is not null)
            .Select(r => r.SuggestedId!.Value)
            .Distinct()
            .ToList();

        var suggestions = await db
            .Products.Where(p => suggestedIds.Contains(p.Id))
            .Select(ProductRow.Projection)
            .ToDictionaryAsync(p => p.Id, ct);

        return
        [
            .. reviews.Select(r => new IngredientReview(
                r.Id,
                r.Text,
                r.Name,
                r.SuggestedId is { } id ? suggestions.GetValueOrDefault(id) : null
            )),
        ];
    }

    public async Task<bool> AssignIngredientAsync(
        int ingredientId,
        int productId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var ingredient = await db.RecipeIngredients.FindAsync([ingredientId], ct);

        if (ingredient is null || !await db.Products.AnyAsync(p => p.Id == productId, ct))
        {
            return false;
        }

        var parsed = RecipeIngestService.Parse(Request(ingredient.FreeText));

        ingredient.ProductId = productId;
        ingredient.QuantityBase = await RecipeIngestService.ToBaseQuantityAsync(
            db,
            productId,
            parsed.Quantity,
            parsed.Unit,
            ct
        );
        ingredient.FreeText = null;
        ingredient.NeedsReview = false;

        // Learn the ingredient name, not the whole line: "250 g Mehl" should teach "Mehl".
        await AliasLearning.LearnAsync(db, parsed.Name, productId, storeId: null, ct);

        await db.SaveChangesAsync(ct);

        return true;
    }

    public async Task KeepAsTextAsync(int ingredientId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db
            .RecipeIngredients.Where(i => i.Id == ingredientId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.NeedsReview, false), ct);
    }

    private static string ParseName(string text) => RecipeIngestService.Parse(Request(text)).Name;

    private static RecipeIngredientRequest Request(string? text) =>
        new(text, null, null, null, null, null);
}
