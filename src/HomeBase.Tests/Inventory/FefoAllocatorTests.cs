using HomeBase.Features.Inventory;

namespace HomeBase.Tests.Inventory;

public class FefoAllocatorTests
{
    [Fact]
    public void Allocate_FirstLotSuffices_TakesOnlyFromFirstLot()
    {
        var allocation = FefoAllocator.Allocate([new(1, 500m), new(2, 500m)], 200m);

        Assert.Equal([new LotTake(1, 200m)], allocation.Takes);
        Assert.Equal(200m, allocation.Applied);
        Assert.Equal(0m, allocation.Shortfall);
    }

    [Fact]
    public void Allocate_SpansLotsInGivenOrder()
    {
        var allocation = FefoAllocator.Allocate(
            [new LotQuantity(7, 100m), new(3, 250m), new(9, 400m)],
            300m
        );

        Assert.Equal([new LotTake(7, 100m), new LotTake(3, 200m)], allocation.Takes);
        Assert.Equal(300m, allocation.Applied);
        Assert.Equal(0m, allocation.Shortfall);
    }

    [Fact]
    public void Allocate_NotEnoughStock_ReportsShortfall()
    {
        var allocation = FefoAllocator.Allocate([new(1, 100m), new(2, 50m)], 200m);

        Assert.Equal([new LotTake(1, 100m), new LotTake(2, 50m)], allocation.Takes);
        Assert.Equal(150m, allocation.Applied);
        Assert.Equal(50m, allocation.Shortfall);
        Assert.False(allocation.ToResult().IsComplete);
    }

    [Fact]
    public void Allocate_SkipsEmptyAndNegativeLots()
    {
        var allocation = FefoAllocator.Allocate([new(1, 0m), new(2, -5m), new(3, 80m)], 50m);

        Assert.Equal([new LotTake(3, 50m)], allocation.Takes);
    }

    [Fact]
    public void Allocate_NoLots_EverythingIsShortfall()
    {
        var allocation = FefoAllocator.Allocate([], 75m);

        Assert.Empty(allocation.Takes);
        Assert.Equal(0m, allocation.Applied);
        Assert.Equal(75m, allocation.Shortfall);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Allocate_NothingRequested_TakesNothing(int quantity)
    {
        var allocation = FefoAllocator.Allocate([new(1, 100m)], quantity);

        Assert.Empty(allocation.Takes);
        Assert.Equal(0m, allocation.Applied);
    }

    [Fact]
    public void Allocate_AppliedPlusShortfall_EqualsRequested()
    {
        var allocation = FefoAllocator.Allocate([new(1, 0.333m), new(2, 0.333m)], 1m);

        Assert.Equal(1m, allocation.Applied + allocation.Shortfall);
        Assert.Equal(allocation.Takes.Sum(t => t.QuantityBase), allocation.Applied);
    }

    [Fact]
    public void ToResult_FullyApplied_IsComplete()
    {
        var result = FefoAllocator.Allocate([new(1, 10m)], 10m).ToResult();

        Assert.True(result.IsComplete);
        Assert.Equal(new StockChangeResult(10m, 0m), result);
    }
}
