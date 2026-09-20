using HomeBase.Database.Entities;

namespace HomeBase.Features.Catalog;

public static class LocationTree
{
    public static IReadOnlyList<StorageLocation> Flatten(IReadOnlyList<StorageLocation> all) =>
        [
            .. all.Where(l => l.ParentId is null)
                .OrderBy(l => l.Name)
                .SelectMany(root =>
                    new[] { root }.Concat(
                        all.Where(c => c.ParentId == root.Id).OrderBy(c => c.Name)
                    )
                ),
        ];

    public static string Label(StorageLocation location) =>
        location.Parent is null ? location.Name : $"{location.Parent.Name} › {location.Name}";
}
