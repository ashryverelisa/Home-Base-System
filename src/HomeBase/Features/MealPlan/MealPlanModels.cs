using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.MealPlan;

public sealed record MealPlanRow(
    long Id,
    DateOnly PlanDate,
    MealSlot Slot,
    int? RecipeId,
    string? RecipeName,
    string? FreeText,
    int Servings,
    MealPlanStatus Status,
    DateTimeOffset? CookedAt
)
{
    public static readonly Expression<Func<MealPlanEntry, MealPlanRow>> Projection =
        entry => new MealPlanRow(
            entry.Id,
            entry.PlanDate,
            entry.Slot,
            entry.RecipeId,
            entry.Recipe!.Name,
            entry.FreeText,
            entry.Servings,
            entry.Status,
            entry.CookedAt
        );

    public string Label => RecipeName ?? FreeText ?? string.Empty;

    public bool IsCooked => Status == MealPlanStatus.Cooked;

    public bool IsPlanned => Status == MealPlanStatus.Planned;
}

public sealed record CookLine(
    int ProductId,
    string Name,
    BaseUnit BaseUnit,
    decimal Needed,
    decimal Available,
    bool IsOptional
)
{
    public decimal Shortfall => Math.Max(0m, Needed - Available);

    public bool IsCovered => Shortfall == 0m;
}

public sealed record CookPlan(
    long EntryId,
    string Label,
    int Servings,
    IReadOnlyList<CookLine> Lines,
    IReadOnlyList<string> Untracked
)
{
    public bool IsFullyCovered => Lines.All(l => l.IsCovered || l.IsOptional);
}

public sealed record CookResult(bool Succeeded, IReadOnlyList<CookLine> Missing)
{
    public static CookResult Ok() => new(true, []);
}

public sealed record NeedRow(
    int? ProductId,
    string Label,
    BaseUnit? BaseUnit,
    decimal Needed,
    decimal Stock
)
{
    public decimal Missing => Math.Max(0m, Needed - Stock);

    public bool IsCovered => ProductId is not null && Missing == 0m;
}
