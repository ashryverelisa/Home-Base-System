namespace HomeBase.Features.MealPlan;

public static class RecipeScaling
{
    public static decimal Scale(decimal quantityBase, int servings, int recipeServings) =>
        quantityBase * servings / Denominator(recipeServings);

    private static int Denominator(int recipeServings) => recipeServings > 0 ? recipeServings : 1;
}
