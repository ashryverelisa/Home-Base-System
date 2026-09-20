using HomeBase.Database.Enums;

namespace HomeBase.Features.MealPlan;

public static class MealNeedsBuilder
{
    public static IReadOnlyList<NeedRow> Build(
        IReadOnlyList<PlannedMeal> meals,
        IReadOnlyList<IngredientLine> ingredients,
        IReadOnlyDictionary<int, decimal> stockTotals
    )
    {
        Dictionary<int, Accumulated> tracked = [];
        HashSet<string> untracked = [];

        foreach (var meal in meals)
        {
            foreach (var ingredient in ingredients.Where(i => i.RecipeId == meal.RecipeId))
            {
                if (ingredient.IsOptional)
                {
                    continue;
                }

                if (!ingredient.IsTracked)
                {
                    if (!string.IsNullOrWhiteSpace(ingredient.Label))
                    {
                        untracked.Add(ingredient.Label);
                    }

                    continue;
                }

                var productId = ingredient.ProductId!.Value;
                var needed = RecipeScaling.Scale(
                    ingredient.QuantityBase!.Value,
                    meal.Servings,
                    meal.RecipeServings
                );

                var current = tracked.GetValueOrDefault(
                    productId,
                    new Accumulated(ingredient.ProductName!, ingredient.BaseUnit!.Value, 0m)
                );

                tracked[productId] = current with { Needed = current.Needed + needed };
            }
        }

        return
        [
            .. tracked
                .Select(t => new NeedRow(
                    t.Key,
                    t.Value.Name,
                    t.Value.Unit,
                    t.Value.Needed,
                    stockTotals.GetValueOrDefault(t.Key)
                ))
                .OrderByDescending(n => n.Missing)
                .ThenBy(n => n.Label),
            .. untracked
                .Select(label => new NeedRow(null, label, null, 0m, 0m))
                .OrderBy(n => n.Label),
        ];
    }

    private sealed record Accumulated(string Name, BaseUnit Unit, decimal Needed);
}
