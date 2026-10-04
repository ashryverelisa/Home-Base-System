using HomeBase.Database.Enums;
using HomeBase.Features.MealPlan;

namespace HomeBase.Tests.MealPlan;

public class CookPlanBuilderTests
{
    private static IngredientLine Tracked(int productId, string name, decimal quantity, bool optional = false) =>
        new(1, productId, name, BaseUnit.Gram, quantity, null, optional);

    private static IngredientLine Untracked(string text) =>
        new(1, null, null, null, null, text, false);

    [Fact]
    public void Build_ScalesIngredientsToPlannedServings()
    {
        var plan = CookPlanBuilder.Build(
            entryId: 42,
            label: "Pfannkuchen",
            servings: 4,
            recipeServings: 2,
            [Tracked(1, "Mehl", 250m)],
            new Dictionary<int, decimal> { [1] = 1000m }
        );

        var line = Assert.Single(plan.Lines);
        Assert.Equal(500m, line.Needed);
        Assert.Equal(1000m, line.Available);
        Assert.Equal(42, plan.EntryId);
        Assert.Equal("Pfannkuchen", plan.Label);
        Assert.Equal(4, plan.Servings);
    }

    [Fact]
    public void Build_UntrackedIngredients_AreListedSeparately()
    {
        var plan = CookPlanBuilder.Build(
            1,
            "Salat",
            2,
            2,
            [Tracked(1, "Gurke", 1m), Untracked("Salz"), Untracked("Pfeffer")],
            new Dictionary<int, decimal>()
        );

        Assert.Single(plan.Lines);
        Assert.Equal(["Salz", "Pfeffer"], plan.Untracked);
    }

    [Fact]
    public void Build_IngredientWithoutQuantity_IsUntracked()
    {
        IngredientLine noQuantity = new(1, 5, "Petersilie", BaseUnit.Gram, null, null, false);

        var plan = CookPlanBuilder.Build(1, "Suppe", 2, 2, [noQuantity], new Dictionary<int, decimal>());

        Assert.Empty(plan.Lines);
        Assert.Equal(["Petersilie"], plan.Untracked);
    }

    [Fact]
    public void Build_MissingStock_CountsAsZeroAvailable()
    {
        var plan = CookPlanBuilder.Build(1, "Kuchen", 2, 2, [Tracked(9, "Butter", 200m)], new Dictionary<int, decimal>());

        var line = Assert.Single(plan.Lines);
        Assert.Equal(0m, line.Available);
        Assert.Equal(200m, line.Shortfall);
        Assert.False(plan.IsFullyCovered);
    }

    [Fact]
    public void IsFullyCovered_OnlyOptionalLinesShort_IsTrue()
    {
        var plan = CookPlanBuilder.Build(
            1,
            "Pasta",
            2,
            2,
            [Tracked(1, "Nudeln", 250m), Tracked(2, "Parmesan", 50m, optional: true)],
            new Dictionary<int, decimal> { [1] = 500m }
        );

        Assert.True(plan.IsFullyCovered);
        Assert.True(plan.Lines[0].IsCovered);
        Assert.False(plan.Lines[1].IsCovered);
    }

    [Fact]
    public void CookLine_Shortfall_IsNeverNegative()
    {
        var line = new CookLine(1, "Reis", BaseUnit.Gram, Needed: 100m, Available: 900m, IsOptional: false);

        Assert.Equal(0m, line.Shortfall);
        Assert.True(line.IsCovered);
    }
}
