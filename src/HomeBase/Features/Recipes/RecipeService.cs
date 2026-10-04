using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Queries;
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
                i.Note
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
}
