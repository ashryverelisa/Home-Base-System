using HomeBase.Features.Common;

namespace HomeBase.Tests.Common;

public class ShelfLifeTests
{
    private static readonly DateOnly BookedOn = new(2026, 3, 10);

    [Fact]
    public void Resolve_EnteredDate_WinsOverDefault()
    {
        var entered = new DateOnly(2026, 4, 1);

        Assert.Equal(entered, ShelfLife.Resolve(entered, 7, BookedOn));
    }

    [Fact]
    public void Resolve_NoEnteredDate_AddsDefaultShelfLife()
    {
        Assert.Equal(new DateOnly(2026, 3, 17), ShelfLife.Resolve(null, 7, BookedOn));
    }

    [Fact]
    public void Resolve_ZeroDays_ReturnsBookingDay()
    {
        Assert.Equal(BookedOn, ShelfLife.Resolve(null, 0, BookedOn));
    }

    [Fact]
    public void Resolve_NothingKnown_ReturnsNull()
    {
        Assert.Null(ShelfLife.Resolve(null, null, BookedOn));
    }

    [Fact]
    public void Learnable_EnteredDateWithoutDefault_ReturnsDaysFromBooking()
    {
        Assert.Equal(21, ShelfLife.Learnable(new DateOnly(2026, 3, 31), null, BookedOn));
    }

    [Fact]
    public void Learnable_SameDay_ReturnsZero()
    {
        Assert.Equal(0, ShelfLife.Learnable(BookedOn, null, BookedOn));
    }

    [Fact]
    public void Learnable_ProductAlreadyHasDefault_ReturnsNull()
    {
        Assert.Null(ShelfLife.Learnable(new DateOnly(2026, 3, 31), 7, BookedOn));
    }

    [Fact]
    public void Learnable_NoDateOrDateInPast_ReturnsNull()
    {
        Assert.Null(ShelfLife.Learnable(null, null, BookedOn));
        Assert.Null(ShelfLife.Learnable(new DateOnly(2026, 3, 9), null, BookedOn));
    }
}
