namespace HomeBase.Features.MealPlan;

public static class PlanWeek
{
    public static DateOnly StartOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
}
