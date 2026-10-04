using HomeBase.Database.Enums;
using HomeBase.Features.MealPlan;

namespace HomeBase.Tests.MealPlan;

public class MealNeedsBuilderTests
{
    private const int Pancakes = 1;
    private const int Pasta = 2;

    private static readonly IngredientLine[] Ingredients =
    [
        new(Pancakes, 10, "Mehl", BaseUnit.Gram, 200m, null, false),
        new(Pancakes, 11, "Milch", BaseUnit.Milliliter, 400m, null, false),
        new(Pancakes, 12, "Zucker", BaseUnit.Gram, 20m, null, IsOptional: true),
        new(Pancakes, null, null, null, null, "Prise Salz", false),
        new(Pasta, 10, "Mehl", BaseUnit.Gram, 100m, null, false),
        new(Pasta, null, null, null, null, "Prise Salz", false),
        new(Pasta, null, null, null, null, "  ", false),
    ];

    [Fact]
    public void Build_SumsSameProductAcrossMealsWithScaling()
    {
        PlannedMeal[] meals = [new(Pancakes, 4, 2), new(Pasta, 2, 2)];

        var needs = MealNeedsBuilder.Build(meals, Ingredients, new Dictionary<int, decimal>());

        var flour = Assert.Single(needs, n => n.ProductId == 10);
        Assert.Equal(400m + 100m, flour.Needed);
        Assert.Equal(BaseUnit.Gram, flour.BaseUnit);
    }

    [Fact]
    public void Build_SameRecipePlannedTwice_CountsTwice()
    {
        PlannedMeal[] meals = [new(Pasta, 2, 2), new(Pasta, 2, 2)];

        var needs = MealNeedsBuilder.Build(meals, Ingredients, new Dictionary<int, decimal>());

        Assert.Equal(200m, Assert.Single(needs, n => n.ProductId == 10).Needed);
    }

    [Fact]
    public void Build_SkipsOptionalIngredients()
    {
        var needs = MealNeedsBuilder.Build([new(Pancakes, 2, 2)], Ingredients, new Dictionary<int, decimal>());

        Assert.DoesNotContain(needs, n => n.ProductId == 12);
    }

    [Fact]
    public void Build_IgnoresIngredientsOfUnplannedRecipes()
    {
        var needs = MealNeedsBuilder.Build([new(Pasta, 2, 2)], Ingredients, new Dictionary<int, decimal>());

        Assert.DoesNotContain(needs, n => n.ProductId == 11);
    }

    [Fact]
    public void Build_UntrackedIngredients_AreDeduplicatedAndListedLast()
    {
        PlannedMeal[] meals = [new(Pancakes, 2, 2), new(Pasta, 2, 2)];

        var needs = MealNeedsBuilder.Build(meals, Ingredients, new Dictionary<int, decimal>());

        var untracked = Assert.Single(needs, n => n.ProductId is null);
        Assert.Equal("Prise Salz", untracked.Label);
        Assert.Same(untracked, needs[^1]);
        Assert.False(untracked.IsCovered);
    }

    [Fact]
    public void Build_OrdersByMissingDescendingThenByLabel()
    {
        IngredientLine[] ingredients =
        [
            new(Pasta, 1, "Basilikum", BaseUnit.Gram, 10m, null, false),
            new(Pasta, 2, "Tomaten", BaseUnit.Gram, 400m, null, false),
            new(Pasta, 3, "Nudeln", BaseUnit.Gram, 500m, null, false),
            new(Pasta, 4, "Knoblauch", BaseUnit.Piece, 2m, null, false),
        ];
        var stock = new Dictionary<int, decimal> { [1] = 50m, [2] = 100m, [3] = 400m, [4] = 5m };

        var needs = MealNeedsBuilder.Build([new(Pasta, 2, 2)], ingredients, stock);

        Assert.Equal(["Tomaten", "Nudeln", "Basilikum", "Knoblauch"], needs.Select(n => n.Label));
        Assert.Equal([300m, 100m, 0m, 0m], needs.Select(n => n.Missing));
        Assert.True(needs[2].IsCovered);
    }

    [Fact]
    public void Build_NoMeals_ReturnsEmpty()
    {
        Assert.Empty(MealNeedsBuilder.Build([], Ingredients, new Dictionary<int, decimal>()));
    }
}
