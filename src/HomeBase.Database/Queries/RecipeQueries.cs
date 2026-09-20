using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class RecipeQueries
{
    public static IQueryable<Recipe> WithIngredients(this IQueryable<Recipe> recipes) =>
        recipes.Include(r => r.Ingredients).ThenInclude(i => i.Product);

    public static IQueryable<Recipe> MatchingSearch(this IQueryable<Recipe> recipes, string term) =>
        recipes.Where(r =>
            EF.Functions.ILike(r.Name, $"%{term}%") || r.Tags.Any(t => EF.Functions.ILike(t, term))
        );

    public static IOrderedQueryable<Recipe> InDisplayOrder(this IQueryable<Recipe> recipes) =>
        recipes.OrderBy(r => r.Name);

    public static IQueryable<RecipeIngredient> ForRecipe(
        this IQueryable<RecipeIngredient> ingredients,
        int recipeId
    ) => ingredients.Where(i => i.RecipeId == recipeId);

    public static IOrderedQueryable<RecipeIngredient> InRecipeOrder(
        this IQueryable<RecipeIngredient> ingredients
    ) => ingredients.OrderBy(i => i.SortOrder).ThenBy(i => i.Id);
}
