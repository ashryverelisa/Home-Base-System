using HomeBase.Features.MealPlan;

namespace HomeBase.Tests.MealPlan;

public class PlanWeekTests
{
    [Theory]
    [InlineData(5)] // Monday
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(11)] // Sunday
    public void StartOf_ReturnsMondayOfSameWeek(int dayOfOctober)
    {
        Assert.Equal(
            new DateOnly(2026, 10, 5),
            PlanWeek.StartOf(new DateOnly(2026, 10, dayOfOctober))
        );
    }

    [Fact]
    public void StartOf_WeekSpanningYearEnd_ReturnsMondayInPreviousYear()
    {
        Assert.Equal(new DateOnly(2025, 12, 29), PlanWeek.StartOf(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void StartOf_IsAlwaysMondayAndAtMostSixDaysBack()
    {
        var day = new DateOnly(2026, 1, 1);

        for (var i = 0; i < 366; i++, day = day.AddDays(1))
        {
            var start = PlanWeek.StartOf(day);

            Assert.Equal(DayOfWeek.Monday, start.DayOfWeek);
            Assert.InRange(day.DayNumber - start.DayNumber, 0, 6);
        }
    }
}
