using HomeBase.Features.MealPlan;

namespace HomeBase.Tests.MealPlan;

public class RecipeScalingTests
{
    [Theory]
    [InlineData(200, 4, 2, 400)]
    [InlineData(200, 1, 2, 100)]
    [InlineData(300, 3, 3, 300)]
    [InlineData(500, 0, 4, 0)]
    public void Scale_ProportionalToServings(int quantity, int servings, int recipeServings, int expected)
    {
        Assert.Equal(expected, RecipeScaling.Scale(quantity, servings, recipeServings));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Scale_InvalidRecipeServings_TreatedAsOneServing(int recipeServings)
    {
        Assert.Equal(600m, RecipeScaling.Scale(200m, 3, recipeServings));
    }

    [Fact]
    public void Scale_KeepsDecimalPrecision()
    {
        Assert.Equal(250m, RecipeScaling.Scale(100m, 5, 2));
        Assert.Equal(100m / 3m, RecipeScaling.Scale(100m, 1, 3));
    }
}
