using HomeBase.Features.Analytics;

namespace HomeBase.Tests.Analytics;

public class PriceTrendTests
{
    private static DateTimeOffset Month(int year, int month) => new(year, month, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CarryForward_NoObservations_ReturnsEmpty()
    {
        Assert.Empty(AnalyticsService.CarryForward([]));
    }

    [Fact]
    public void CarryForward_SingleMonth_ReturnsSinglePoint()
    {
        var trend = AnalyticsService.CarryForward(new() { [Month(2026, 5)] = 1.5m });

        Assert.Equal([new PriceTrendPoint(Month(2026, 5), 1.5m)], trend);
    }

    [Fact]
    public void CarryForward_FillsGapsWithLastKnownPrice()
    {
        var trend = AnalyticsService.CarryForward(
            new()
            {
                [Month(2026, 4)] = 2.0m,
                [Month(2026, 1)] = 1.0m,
            }
        );

        Assert.Equal(
            [
                new PriceTrendPoint(Month(2026, 1), 1.0m),
                new PriceTrendPoint(Month(2026, 2), 1.0m),
                new PriceTrendPoint(Month(2026, 3), 1.0m),
                new PriceTrendPoint(Month(2026, 4), 2.0m),
            ],
            trend
        );
    }

    [Fact]
    public void CarryForward_CrossesYearBoundary()
    {
        var trend = AnalyticsService.CarryForward(
            new()
            {
                [Month(2025, 11)] = 3m,
                [Month(2026, 2)] = 4m,
            }
        );

        Assert.Equal(
            [Month(2025, 11), Month(2025, 12), Month(2026, 1), Month(2026, 2)],
            trend.Select(p => p.Month)
        );
    }

    [Theory]
    [InlineData(2.0, 1.5, -0.5, true, false)]
    [InlineData(1.5, 2.0, 0.5, false, true)]
    [InlineData(1.5, 1.5, 0.0, false, false)]
    public void ProductPriceRow_ComparesLastWithPreviousPrice(
        double previous,
        double last,
        double change,
        bool cheaper,
        bool pricier
    )
    {
        var row = new ProductPriceRow(1, "Kaffee", default, 10m, (decimal)last, (decimal)previous, null);

        Assert.Equal((decimal)change, row.Change);
        Assert.Equal(cheaper, row.IsCheaper);
        Assert.Equal(pricier, row.IsPricier);
    }

    [Fact]
    public void ProductPriceRow_WithoutPreviousPrice_HasNoChange()
    {
        var row = new ProductPriceRow(1, "Kaffee", default, 10m, 1m, null, null);

        Assert.Null(row.Change);
        Assert.False(row.IsCheaper);
        Assert.False(row.IsPricier);
    }

    [Fact]
    public void PromoSavingRow_TotalAddsDiscountAndSaving()
    {
        Assert.Equal(7.5m, new PromoSavingRow(Month(2026, 1), 5m, 2.5m).Total);
    }

    [Theory]
    [InlineData(7, true)]
    [InlineData(7.1, false)]
    [InlineData(0, true)]
    public void ReachRow_RunsOutSoon_WithinAWeek(double daysLeft, bool expected)
    {
        Assert.Equal(expected, new ReachRow(1, "Milch", default, 1m, 1m, (decimal)daysLeft).RunsOutSoon);
    }
}
