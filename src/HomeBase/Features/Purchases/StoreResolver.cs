using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Purchases;

// Manual purchases and n8n receipts name their store as text; unknown names become new stores.
public static class StoreResolver
{
    public static async Task<int?> FindOrCreateAsync(
        HomeBaseDbContext db,
        string? name,
        string? taxId,
        CancellationToken ct
    )
    {
        if (taxId is { Length: > 0 })
        {
            var byTaxId = await db.Stores.FirstOrDefaultAsync(s => s.TaxId == taxId, ct);

            if (byTaxId is not null)
            {
                return byTaxId.Id;
            }
        }

        if (name.TrimToNull() is not { } trimmed)
        {
            return null;
        }

        var existing = await db.Stores.ByNameAsync(trimmed, ct);

        if (existing is not null)
        {
            return existing.Id;
        }

        var store = new Store { Name = trimmed, TaxId = taxId };

        db.Stores.Add(store);
        await db.SaveChangesAsync(ct);

        return store.Id;
    }
}
