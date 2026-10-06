using HomeBase.Database.Enums;
using HomeBase.Features.MasterData;

namespace HomeBase.Tests.MasterData;

public class TwoLevelTreeTests
{
    private static readonly LocationRow Kitchen = Row(1, "Küche");
    private static readonly LocationRow Fridge = Row(2, "Kühlschrank", parentId: 1);
    private static readonly LocationRow Pantry = Row(3, "Vorratsregal", parentId: 1);
    private static readonly LocationRow Cellar = Row(4, "Keller");
    private static readonly LocationRow Bath = Row(5, "Bad");

    private static readonly LocationRow[] All = [Pantry, Bath, Fridge, Cellar, Kitchen];

    [Fact]
    public void Ordered_PutsChildrenDirectlyBelowTheirRoot_AlphabeticallyOnEachLevel()
    {
        Assert.Equal(
            ["Bad", "Keller", "Küche", "Kühlschrank", "Vorratsregal"],
            TwoLevelTree.Ordered(All).Select(r => r.Name)
        );
    }

    [Fact]
    public void ParentOptions_OffersOnlyRoots_WithoutTheNodeItself()
    {
        var options = TwoLevelTree.ParentOptions(All, Cellar.Id, nodeHasChildren: false);

        Assert.Equal(["Bad", "Küche"], options.Select(r => r.Name));
    }

    [Fact]
    public void ParentOptions_NewNode_OffersAllRoots()
    {
        var options = TwoLevelTree.ParentOptions(All, 0, nodeHasChildren: false);

        Assert.Equal(["Bad", "Keller", "Küche"], options.Select(r => r.Name));
    }

    [Fact]
    public void ParentOptions_NodeWithChildren_StaysRoot()
    {
        Assert.Empty(TwoLevelTree.ParentOptions(All, Kitchen.Id, nodeHasChildren: true));
    }

    [Theory]
    [InlineData(4, false, null, null, true)]
    [InlineData(4, false, 1, null, true)]
    [InlineData(0, false, 1, null, true)]
    [InlineData(4, false, 2, 1, false)]
    [InlineData(1, true, 4, null, false)]
    [InlineData(4, false, 4, null, false)]
    public void CanAttach_EnforcesTwoLevels(
        int nodeId,
        bool nodeHasChildren,
        int? parentId,
        int? parentsParentId,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            TwoLevelTree.CanAttach(nodeId, nodeHasChildren, parentId, parentsParentId)
        );
    }

    private static LocationRow Row(int id, string name, int? parentId = null) =>
        new(id, name, parentId, StorageZone.Ambient, 0, 0, 0);
}
