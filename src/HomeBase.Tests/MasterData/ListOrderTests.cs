using HomeBase.Features.MasterData;

namespace HomeBase.Tests.MasterData;

public class ListOrderTests
{
    private static readonly int[] Lists = [10, 20, 30, 40];

    [Fact]
    public void Move_Up_SwapsWithPredecessor()
    {
        Assert.Equal([10, 30, 20, 40], ListOrder.Move(Lists, 30, -1));
    }

    [Fact]
    public void Move_Down_SwapsWithSuccessor()
    {
        Assert.Equal([10, 30, 20, 40], ListOrder.Move(Lists, 20, 1));
    }

    [Theory]
    [InlineData(10, -1)]
    [InlineData(40, 1)]
    public void Move_PastTheEdge_KeepsOrder(int id, int offset)
    {
        Assert.Equal(Lists, ListOrder.Move(Lists, id, offset));
    }

    [Fact]
    public void Move_UnknownId_KeepsOrder()
    {
        Assert.Equal(Lists, ListOrder.Move(Lists, 99, 1));
    }
}
