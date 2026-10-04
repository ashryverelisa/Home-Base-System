using HomeBase.Database.Entities;
using HomeBase.Features.Catalog;

namespace HomeBase.Tests.Catalog;

public class LocationTreeTests
{
    [Fact]
    public void Flatten_RootsSortedWithTheirChildrenDirectlyBelow()
    {
        StorageLocation[] all =
        [
            new() { Id = 1, Name = "Küche" },
            new() { Id = 2, Name = "Keller" },
            new() { Id = 3, Name = "Vorratsschrank", ParentId = 1 },
            new() { Id = 4, Name = "Kühlschrank", ParentId = 1 },
            new() { Id = 5, Name = "Regal", ParentId = 2 },
        ];

        var flat = LocationTree.Flatten(all);

        Assert.Equal(["Keller", "Regal", "Küche", "Kühlschrank", "Vorratsschrank"], flat.Select(l => l.Name));
    }

    [Fact]
    public void Flatten_ChildWithoutKnownParent_IsLeftOut()
    {
        StorageLocation[] all =
        [
            new() { Id = 1, Name = "Küche" },
            new() { Id = 2, Name = "Verwaist", ParentId = 99 },
        ];

        Assert.Equal(["Küche"], LocationTree.Flatten(all).Select(l => l.Name));
    }

    [Fact]
    public void Flatten_Empty_ReturnsEmpty()
    {
        Assert.Empty(LocationTree.Flatten([]));
    }

    [Fact]
    public void Label_IncludesParentName()
    {
        var kitchen = new StorageLocation { Id = 1, Name = "Küche" };
        var fridge = new StorageLocation { Id = 2, Name = "Kühlschrank", ParentId = 1, Parent = kitchen };

        Assert.Equal("Küche", LocationTree.Label(kitchen));
        Assert.Equal("Küche › Kühlschrank", LocationTree.Label(fridge));
    }
}
