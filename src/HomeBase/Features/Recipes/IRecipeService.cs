using HomeBase.Database.Entities;
using HomeBase.Features.Common;

namespace HomeBase.Features.Recipes;

public interface IRecipeService
{
    Task<IReadOnlyList<RecipeRow>> SearchAsync(string? term = null, CancellationToken ct = default);

    Task<Recipe?> FindAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<RecipeIngredientRow>> GetIngredientsAsync(
        int recipeId,
        CancellationToken ct = default
    );

    Task<SaveResult<int>> SaveAsync(Recipe recipe, CancellationToken ct = default);

    Task DeleteAsync(int recipeId, CancellationToken ct = default);

    Task<int> AddIngredientAsync(
        int recipeId,
        int? productId,
        string? freeText,
        decimal? quantityBase,
        bool isOptional = false,
        string? note = null,
        CancellationToken ct = default
    );

    Task RemoveIngredientAsync(int ingredientId, CancellationToken ct = default);

    Task<IReadOnlyList<IngredientReview>> GetReviewAsync(
        int recipeId,
        CancellationToken ct = default
    );

    Task<bool> AssignIngredientAsync(
        int ingredientId,
        int productId,
        CancellationToken ct = default
    );

    Task KeepAsTextAsync(int ingredientId, CancellationToken ct = default);
}
