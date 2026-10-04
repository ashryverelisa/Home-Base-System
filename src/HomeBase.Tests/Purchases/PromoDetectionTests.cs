using HomeBase.Database.Enums;
using HomeBase.Features.Purchases;

namespace HomeBase.Tests.Purchases;

public class PromoDetectionTests
{
    private static ReviewLineRow Line(long id, int? productId, decimal? pricePerBaseUnit) =>
        new(
            id,
            (int)id,
            PurchaseLineType.Item,
            $"Line {id}",
            null,
            productId,
            null,
            null,
            null,
            MatchStatus.Manual,
            null,
            1m,
            null,
            1m,
            pricePerBaseUnit,
            false,
            null
        );

    [Fact]
    public void Median_OddCount_ReturnsMiddleValue()
    {
        Assert.Equal(3m, PromoDetection.Median([5m, 1m, 3m]));
    }

    [Fact]
    public void Median_EvenCount_AveragesTheTwoMiddleValues()
    {
        Assert.Equal(2.5m, PromoDetection.Median([4m, 1m, 3m, 2m]));
    }

    [Fact]
    public void Median_SingleValue_ReturnsIt()
    {
        Assert.Equal(1.99m, PromoDetection.Median([1.99m]));
    }

    [Fact]
    public void Median_IsRobustAgainstOutliers()
    {
        Assert.Equal(1m, PromoDetection.Median([1m, 1m, 1m, 1m, 100m]));
    }

    [Theory]
    [InlineData(0.84, true)]
    [InlineData(0.85, false)]
    [InlineData(1.00, false)]
    [InlineData(1.20, false)]
    public void IsHint_TriggersOnlyBelowFifteenPercentUnderMedian(double price, bool expected)
    {
        Assert.Equal(expected, PromoDetection.IsHint((decimal)price, 1m));
    }

    [Fact]
    public void IsHint_LineWithoutProductOrPrice_IsNoHint()
    {
        var medians = new Dictionary<int, decimal> { [1] = 10m };

        Assert.False(PromoDetection.IsHint(Line(1, null, 1m), medians));
        Assert.False(PromoDetection.IsHint(Line(2, 1, null), medians));
    }

    [Fact]
    public void IsHint_ProductWithoutPriceHistory_IsNoHint()
    {
        Assert.False(PromoDetection.IsHint(Line(1, 7, 0.01m), new Dictionary<int, decimal>()));
    }

    [Fact]
    public void Apply_MarksOnlyCheapLinesAndKeepsOrder()
    {
        var medians = new Dictionary<int, decimal> { [1] = 2m, [2] = 5m };
        ReviewLineRow[] rows = [Line(1, 1, 1m), Line(2, 2, 5m), Line(3, null, 0.1m)];

        var result = PromoDetection.Apply(rows, medians);

        Assert.Equal([1L, 2L, 3L], result.Select(r => r.Id));
        Assert.Equal([true, false, false], result.Select(r => r.PromoHint));
        Assert.False(rows[0].PromoHint);
    }

    [Fact]
    public void MediansByProduct_GroupsHistoryPerProduct()
    {
        ProductPrice[] history =
        [
            new(1, 1.0m),
            new(1, 3.0m),
            new(1, 2.0m),
            new(2, 0.5m),
            new(2, 1.5m),
        ];

        var medians = PromoDetection.MediansByProduct(history);

        Assert.Equal(2, medians.Count);
        Assert.Equal(2.0m, medians[1]);
        Assert.Equal(1.0m, medians[2]);
    }
}
