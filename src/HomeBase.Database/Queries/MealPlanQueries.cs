using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class MealPlanQueries
{
    public static IQueryable<MealPlanEntry> Between(
        this IQueryable<MealPlanEntry> entries,
        DateOnly from,
        DateOnly to
    ) => entries.Where(e => e.PlanDate >= from && e.PlanDate <= to);

    public static IQueryable<MealPlanEntry> Planned(this IQueryable<MealPlanEntry> entries) =>
        entries.Where(e => e.Status == MealPlanStatus.Planned);

    public static IOrderedQueryable<MealPlanEntry> InPlanOrder(
        this IQueryable<MealPlanEntry> entries
    ) => entries.OrderBy(e => e.PlanDate).ThenBy(e => e.Slot);

    public static IQueryable<MealPlanEntry> WithRecipe(this IQueryable<MealPlanEntry> entries) =>
        entries.Include(e => e.Recipe);
}
