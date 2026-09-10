using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class StorageLocationQueries
{
    public static IQueryable<StorageLocation> WithParent(
        this IQueryable<StorageLocation> locations
    ) => locations.Include(l => l.Parent);

    public static async Task<HashSet<int>> BranchIdsAsync(
        this IQueryable<StorageLocation> locations,
        int rootId,
        CancellationToken ct = default
    )
    {
        var edges = await locations.Select(l => new { l.Id, l.ParentId }).ToListAsync(ct);

        var branch = new HashSet<int> { rootId };
        var grew = true;

        while (grew)
        {
            grew = false;

            foreach (var edge in edges)
            {
                if (edge.ParentId is { } parent && branch.Contains(parent) && branch.Add(edge.Id))
                {
                    grew = true;
                }
            }
        }

        return branch;
    }
}
