using HomeBase.Database;
using HomeBase.Database.Entities;

namespace HomeBase.Features.Purchases;

// Discount lines point at the item they reduce. The item ids only exist after the first save,
// so the links are set in a second pass from the line positions.
public static class DiscountLinker
{
    public static async Task LinkAsync(
        HomeBaseDbContext db,
        IReadOnlyList<PurchaseItem> items,
        IReadOnlyList<int?> parentIndexes,
        CancellationToken ct
    )
    {
        var linked = false;

        for (var index = 0; index < parentIndexes.Count; index++)
        {
            if (parentIndexes[index] is not { } parent || parent < 0 || parent >= items.Count)
            {
                continue;
            }

            items[index].ParentItemId = items[parent].Id;
            linked = true;
        }

        if (linked)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
