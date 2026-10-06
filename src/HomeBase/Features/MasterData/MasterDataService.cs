using System.Linq.Expressions;
using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.MasterData;

public sealed class MasterDataService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IStringLocalizer<AppStrings> localizer
) : IMasterDataService
{
    public async Task<IReadOnlyList<LocationRow>> GetLocationsAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .StorageLocations.Select(l => new LocationRow(
                l.Id,
                l.Name,
                l.ParentId,
                l.Zone,
                l.Children.Count,
                db.StockLots.Count(s => s.LocationId == l.Id && s.QuantityBase > 0),
                db.Assets.Count(a => a.LocationId == l.Id)
            ))
            .ToListAsync(ct);

        return TwoLevelTree.Ordered(rows);
    }

    public Task<SaveResult> SaveLocationAsync(
        StorageLocation input,
        CancellationToken ct = default
    ) =>
        SaveNodeAsync(
            input,
            db => db.StorageLocations,
            name => new StorageLocation { Name = name },
            location => location.Zone = input.Zone,
            ct
        );

    // Empty lots and product defaults lose the reference via ON DELETE SET NULL.
    public Task<SaveResult> DeleteLocationAsync(int id, CancellationToken ct = default) =>
        DeleteNodeAsync(
            id,
            db => db.StorageLocations,
            db =>
                l => new NodeUsage(
                    l.Children.Count,
                    db.StockLots.Count(s => s.LocationId == l.Id && s.QuantityBase > 0),
                    db.Assets.Count(a => a.LocationId == l.Id)
                ),
            "MasterData.LocationInUse",
            ct
        );

    public async Task<IReadOnlyList<CategoryRow>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .Categories.Select(c => new CategoryRow(
                c.Id,
                c.Name,
                c.ParentId,
                c.Kind,
                c.Children.Count,
                db.Products.Count(p => p.CategoryId == c.Id),
                db.Assets.Count(a => a.CategoryId == c.Id)
            ))
            .ToListAsync(ct);

        return TwoLevelTree.Ordered(rows);
    }

    public Task<SaveResult> SaveCategoryAsync(Category input, CancellationToken ct = default) =>
        SaveNodeAsync(
            input,
            db => db.Categories,
            name => new Category { Name = name },
            category => category.Kind = input.Kind,
            ct
        );

    public Task<SaveResult> DeleteCategoryAsync(int id, CancellationToken ct = default) =>
        DeleteNodeAsync(
            id,
            db => db.Categories,
            db =>
                c => new NodeUsage(
                    c.Children.Count,
                    db.Products.Count(p => p.CategoryId == c.Id),
                    db.Assets.Count(a => a.CategoryId == c.Id)
                ),
            "MasterData.CategoryInUse",
            ct
        );

    public async Task<IReadOnlyList<StoreRow>> GetStoresAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .Stores.InDisplayOrder()
            .Select(s => new StoreRow(
                s.Id,
                s.Name,
                s.Chain,
                s.Address,
                s.IsOnline,
                s.TaxId,
                db.Purchases.Count(p => p.StoreId == s.Id)
            ))
            .ToListAsync(ct);
    }

    public async Task<SaveResult> SaveStoreAsync(Store input, CancellationToken ct = default)
    {
        var name = input.Name.Trim();

        if (name.Length == 0)
        {
            return Failed("MasterData.NameRequired");
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        if (
            await db.Stores.AnyAsync(
                s => s.Id != input.Id && EF.Functions.ILike(s.Name, name),
                ct
            )
        )
        {
            return Failed("MasterData.NameTaken", name);
        }

        var store = input.Id == 0
            ? db.Stores.Add(new Store { Name = name }).Entity
            : await db.Stores.FindAsync([input.Id], ct);

        if (store is null)
        {
            return Failed("MasterData.NotFound");
        }

        store.Name = name;
        store.Chain = input.Chain.TrimToNull();
        store.Address = input.Address.TrimToNull();
        store.IsOnline = input.IsOnline;
        store.TaxId = input.TaxId.TrimToNull();

        await db.SaveChangesAsync(ct);

        return SaveResult.Ok();
    }

    public async Task<SaveResult> DeleteStoreAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var purchases = await db.Purchases.CountAsync(p => p.StoreId == id, ct);

        if (purchases > 0)
        {
            return Failed("MasterData.StoreInUse", purchases);
        }

        // Store-specific aliases go with the store (ON DELETE CASCADE).
        await db.Stores.Where(s => s.Id == id).ExecuteDeleteAsync(ct);

        return SaveResult.Ok();
    }

    public async Task<IReadOnlyList<ShoppingListAdminRow>> GetShoppingListsAsync(
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .ShoppingLists.InDisplayOrder()
            .Select(l => new ShoppingListAdminRow(
                l.Id,
                l.Name,
                l.Kind,
                l.IsDefault,
                l.SortOrder,
                l.Archived,
                0
            ))
            .ToListAsync(ct);

        var counts = await db.ShoppingListItems.OpenCountsByListAsync(ct);

        return [.. rows.Select(r => r with { OpenCount = counts.GetValueOrDefault(r.Id) })];
    }

    public async Task<SaveResult> SaveShoppingListAsync(
        ShoppingList input,
        CancellationToken ct = default
    )
    {
        var name = input.Name.Trim();

        if (name.Length == 0)
        {
            return Failed("MasterData.NameRequired");
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        if (await db.ShoppingLists.AnyAsync(l => l.Id != input.Id && l.Name == name, ct))
        {
            return Failed("MasterData.NameTaken", name);
        }

        ShoppingList? list;

        if (input.Id == 0)
        {
            var last = await db.ShoppingLists.MaxAsync(l => (int?)l.SortOrder, ct);
            list = db.ShoppingLists.Add(new ShoppingList { Name = name, SortOrder = (last ?? -1) + 1 })
                .Entity;
        }
        else
        {
            list = await db.ShoppingLists.FindAsync([input.Id], ct);
        }

        if (list is null)
        {
            return Failed("MasterData.NotFound");
        }

        list.Name = name;
        list.Kind = input.Kind;

        await db.SaveChangesAsync(ct);

        return SaveResult.Ok();
    }

    public async Task<SaveResult> SetDefaultShoppingListAsync(
        int id,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lists = await db.ShoppingLists.ToListAsync(ct);

        if (lists.FirstOrDefault(l => l.Id == id) is not { } chosen)
        {
            return Failed("MasterData.NotFound");
        }

        if (chosen.Archived)
        {
            return Failed("MasterData.ArchivedCannotBeDefault");
        }

        foreach (var list in lists)
        {
            list.IsDefault = list.Id == id;
        }

        await db.SaveChangesAsync(ct);

        return SaveResult.Ok();
    }

    public async Task<SaveResult> SetShoppingListArchivedAsync(
        int id,
        bool archived,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        if (await db.ShoppingLists.FindAsync([id], ct) is not { } list)
        {
            return Failed("MasterData.NotFound");
        }

        if (archived && list.IsDefault)
        {
            return Failed("MasterData.DefaultCannotBeArchived");
        }

        list.Archived = archived;

        await db.SaveChangesAsync(ct);

        return SaveResult.Ok();
    }

    public async Task MoveShoppingListAsync(int id, int offset, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lists = await db.ShoppingLists.InDisplayOrder().ToListAsync(ct);
        var order = ListOrder.Move([.. lists.Select(l => l.Id)], id, offset);

        for (var position = 0; position < order.Count; position++)
        {
            lists.First(l => l.Id == order[position]).SortOrder = position;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<SaveResult> SaveNodeAsync<T>(
        T input,
        Func<HomeBaseDbContext, DbSet<T>> nodes,
        Func<string, T> create,
        Action<T> copyDetails,
        CancellationToken ct
    )
        where T : class, ITreeNode
    {
        var name = input.Name.Trim();
        var id = input.Id;
        var parentId = input.ParentId;

        if (name.Length == 0)
        {
            return Failed("MasterData.NameRequired");
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        var set = nodes(db);

        if (!await CanAttachAsync(set, id, parentId, ct))
        {
            return Failed("MasterData.InvalidParent");
        }

        if (await set.AnyAsync(n => n.Id != id && n.ParentId == parentId && n.Name == name, ct))
        {
            return Failed("MasterData.NameTaken", name);
        }

        var node = id == 0 ? set.Add(create(name)).Entity : await set.FindAsync([id], ct);

        if (node is null)
        {
            return Failed("MasterData.NotFound");
        }

        node.Name = name;
        node.ParentId = parentId;
        copyDetails(node);

        await db.SaveChangesAsync(ct);

        return SaveResult.Ok();
    }

    private async Task<SaveResult> DeleteNodeAsync<T>(
        int id,
        Func<HomeBaseDbContext, DbSet<T>> nodes,
        Func<HomeBaseDbContext, Expression<Func<T, NodeUsage>>> usage,
        string inUseKey,
        CancellationToken ct
    )
        where T : class, ITreeNode
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var set = nodes(db);

        var found = await set.Where(n => n.Id == id).Select(usage(db)).FirstOrDefaultAsync(ct);

        if (found is null)
        {
            return Failed("MasterData.NotFound");
        }

        if (found.Children > 0)
        {
            return Failed("MasterData.HasChildren");
        }

        if (found.First > 0 || found.Second > 0)
        {
            return Failed(inUseKey, found.First, found.Second);
        }

        await set.Where(n => n.Id == id).ExecuteDeleteAsync(ct);

        return SaveResult.Ok();
    }

    private static async Task<bool> CanAttachAsync<T>(
        IQueryable<T> nodes,
        int nodeId,
        int? parentId,
        CancellationToken ct
    )
        where T : class, ITreeNode
    {
        if (parentId is null)
        {
            return true;
        }

        var edges = await nodes
            .Where(n => n.Id == parentId || n.ParentId == nodeId)
            .Select(n => new { n.Id, n.ParentId })
            .ToListAsync(ct);

        var parent = edges.FirstOrDefault(e => e.Id == parentId);
        var hasChildren = nodeId != 0 && edges.Any(e => e.ParentId == nodeId);

        return parent is not null
            && TwoLevelTree.CanAttach(nodeId, hasChildren, parentId, parent.ParentId);
    }

    private SaveResult Failed(string key, params object[] arguments) =>
        SaveResult.Failed(localizer[key, arguments]);

    // Children plus the two kinds of records that still point at a location or category.
    private sealed record NodeUsage(int Children, int First, int Second);
}
