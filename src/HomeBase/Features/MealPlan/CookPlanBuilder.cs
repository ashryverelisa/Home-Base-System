namespace HomeBase.Features.MealPlan;

public static class CookPlanBuilder
{
    public static CookPlan Build(
        long entryId,
        string label,
        int servings,
        int recipeServings,
        IReadOnlyList<IngredientLine> ingredients,
        IReadOnlyDictionary<int, decimal> stockTotals
    )
    {
        List<CookLine> lines = [];
        List<string> untracked = [];

        foreach (var ingredient in ingredients)
        {
            if (!ingredient.IsTracked)
            {
                untracked.Add(ingredient.Label);

                continue;
            }

            lines.Add(
                new CookLine(
                    ingredient.ProductId!.Value,
                    ingredient.ProductName!,
                    ingredient.BaseUnit!.Value,
                    RecipeScaling.Scale(ingredient.QuantityBase!.Value, servings, recipeServings),
                    stockTotals.GetValueOrDefault(ingredient.ProductId!.Value),
                    ingredient.IsOptional
                )
            );
        }

        return new CookPlan(entryId, label, servings, lines, untracked);
    }
}
