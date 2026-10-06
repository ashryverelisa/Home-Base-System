using System.Linq.Expressions;
using HomeBase.Database.Entities;

namespace HomeBase.Features.Recipes;

public sealed record RecipeRow(
    int Id,
    string Name,
    int Servings,
    int? PrepMinutes,
    int? CookMinutes,
    string? ImageUrl,
    List<string> Tags,
    int IngredientCount,
    int ReviewCount
)
{
    public static readonly Expression<Func<Recipe, RecipeRow>> Projection =
        recipe => new RecipeRow(
            recipe.Id,
            recipe.Name,
            recipe.Servings,
            recipe.PrepMinutes,
            recipe.CookMinutes,
            recipe.ImageUrl,
            recipe.Tags,
            recipe.Ingredients.Count,
            recipe.Ingredients.Count(i => i.NeedsReview)
        );

    public int? TotalMinutes =>
        PrepMinutes is null && CookMinutes is null ? null : (PrepMinutes ?? 0) + (CookMinutes ?? 0);
}
