using HomeBase.Database.Enums;

namespace HomeBase.Features.MealPlan;

public interface IMealPlanService
{
    Task<IReadOnlyList<MealPlanRow>> GetRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    );

    Task<long> AddAsync(
        DateOnly date,
        MealSlot slot,
        int? recipeId,
        string? freeText,
        int servings,
        MealPlanStatus status = MealPlanStatus.Planned,
        CancellationToken ct = default
    );

    Task RemoveAsync(long entryId, CancellationToken ct = default);

    Task SetStatusAsync(long entryId, MealPlanStatus status, CancellationToken ct = default);

    Task<CookPlan?> GetCookPlanAsync(long entryId, CancellationToken ct = default);

    Task<CookResult> CookAsync(
        long entryId,
        IReadOnlyList<CookLine> lines,
        CancellationToken ct = default
    );

    Task<decimal> GetCostAsync(long entryId, CancellationToken ct = default);

    Task<IReadOnlyList<NeedRow>> GetNeedsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    );

    Task<int> ApplyToShoppingListAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
