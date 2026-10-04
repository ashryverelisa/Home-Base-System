using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Features.Shopping;

namespace HomeBase.Tests.Shopping;

public class AutoRestockRulesTests
{
    private static readonly ShoppingList Groceries = new() { Id = 1, Name = "Lebensmittel", Kind = ShoppingListKind.Groceries };
    private static readonly ShoppingList Tech = new() { Id = 2, Name = "Technik", Kind = ShoppingListKind.Tech };
    private static readonly ShoppingList Fallback = new() { Id = 3, Name = "Allgemein", IsDefault = true };

    private static ShoppingService.RestockCandidate Candidate(bool isFood, CategoryKind? category) =>
        new(1, 1m, null, 1m, isFood, category);

    [Theory]
    [InlineData(300, 250, 500)]
    [InlineData(500, 250, 500)]
    [InlineData(1, 250, 250)]
    [InlineData(0.5, 1, 1)]
    public void WholePackages_RoundsUpToFullPackages(double missing, double packageSize, double expected)
    {
        Assert.Equal((decimal)expected, ShoppingService.WholePackages((decimal)missing, (decimal)packageSize));
    }

    [Fact]
    public void WholePackages_NoPackageSize_ReturnsMissingAsIs()
    {
        Assert.Equal(123m, ShoppingService.WholePackages(123m, 0m));
    }

    [Fact]
    public void TargetList_FoodProduct_GoesToGroceries()
    {
        var list = ShoppingService.TargetList([Fallback, Tech, Groceries], Candidate(true, CategoryKind.Tech));

        Assert.Same(Groceries, list);
    }

    [Theory]
    [InlineData(CategoryKind.Food, 1)]
    [InlineData(CategoryKind.Tech, 2)]
    public void TargetList_UsesCategoryKind(CategoryKind category, int expectedListId)
    {
        var list = ShoppingService.TargetList([Fallback, Tech, Groceries], Candidate(false, category));

        Assert.Equal(expectedListId, list.Id);
    }

    [Theory]
    [InlineData(CategoryKind.Household)]
    [InlineData(CategoryKind.Other)]
    [InlineData(null)]
    public void TargetList_NoMatchingList_FallsBackToDefault(CategoryKind? category)
    {
        var list = ShoppingService.TargetList([Groceries, Tech, Fallback], Candidate(false, category));

        Assert.Same(Fallback, list);
    }

    [Fact]
    public void TargetList_NoDefaultList_FallsBackToFirst()
    {
        var list = ShoppingService.TargetList([Tech, Groceries], Candidate(false, CategoryKind.Household));

        Assert.Same(Tech, list);
    }
}
