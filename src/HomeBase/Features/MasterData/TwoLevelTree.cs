namespace HomeBase.Features.MasterData;

public static class TwoLevelTree
{
    public static bool CanAttach(
        int nodeId,
        bool nodeHasChildren,
        int? parentId,
        int? parentsParentId
    ) => parentId is null || (parentId != nodeId && parentsParentId is null && !nodeHasChildren);

    public static IReadOnlyList<T> ParentOptions<T>(
        IEnumerable<T> all,
        int nodeId,
        bool nodeHasChildren
    )
        where T : ITreeRow =>
        nodeHasChildren
            ? []
            : [.. all.Where(r => r.ParentId is null && r.Id != nodeId).OrderBy(r => r.Name)];

    public static IReadOnlyList<T> Ordered<T>(IReadOnlyList<T> all)
        where T : ITreeRow =>
        [
            .. all.Where(r => r.ParentId is null)
                .OrderBy(r => r.Name)
                .SelectMany(root =>
                    new[] { root }.Concat(
                        all.Where(c => c.ParentId == root.Id).OrderBy(c => c.Name)
                    )
                ),
        ];
}
