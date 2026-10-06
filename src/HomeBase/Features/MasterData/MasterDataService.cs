using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Queries;
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

    public async Task<MasterDataResult> SaveLocationAsync(
        StorageLocation input,
        CancellationToken ct = default
    )
    {
        var name = input.Name.Trim();

        if (name.Length == 0)
        {
            return Failed("MasterData.NameRequired");
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        if (!await CanAttachAsync(db.StorageLocations, input.Id, input.ParentId, ct))
        {
            return Failed("MasterData.InvalidParent");
        }

        if (
            await db.StorageLocations.AnyAsync(
                l => l.Id != input.Id && l.ParentId == input.ParentId && l.Name == name,
                ct
            )
        )
        {
            return Failed("MasterData.NameTaken", name);
        }

        var location = input.Id == 0
            ? db.StorageLocations.Add(new StorageLocation { Name = name }).Entity
            : await db.StorageLocations.FindAsync([input.Id], ct);

        if (location is null)
        {
            return Failed("MasterData.NotFound");
        }

        location.Name = name;
        location.ParentId = input.ParentId;
        location.Zone = input.Zone;

        await db.SaveChangesAsync(ct);

        return MasterDataResult.Ok;
    }

    public async Task<MasterDataResult> DeleteLocationAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var usage = await db
            .StorageLocations.Where(l => l.Id == id)
            .Select(l => new
            {
                Children = l.Children.Count,
                Lots = db.StockLots.Count(s => s.LocationId == l.Id && s.QuantityBase > 0),
                Assets = db.Assets.Count(a => a.LocationId == l.Id),
            })
            .FirstOrDefaultAsync(ct);

        if (usage is null)
        {
            return Failed("MasterData.NotFound");
        }

        if (usage.Children > 0)
        {
            return Failed("MasterData.HasChildren");
        }

        if (usage.Lots > 0 || usage.Assets > 0)
        {
            return Failed("MasterData.LocationInUse", usage.Lots, usage.Assets);
        }

        // Empty lots and product defaults lose the reference via ON DELETE SET NULL.
        await db.StorageLocations.Where(l => l.Id == id).ExecuteDeleteAsync(ct);

        return MasterDataResult.Ok;
    }

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

    public async Task<MasterDataResult> SaveCategoryAsync(
        Category input,
        CancellationToken ct = default
    )
    {
        var name = input.Name.Trim();

        if (name.Length == 0)
        {
            return Failed("MasterData.NameRequired");
        }

        await using var db = await factory.CreateDbContextAsync(ct);

        if (!await CanAttachAsync(db.Categories, input.Id, input.ParentId, ct))
        {
            return Failed("MasterData.InvalidParent");
        }

        if (
            await db.Categories.AnyAsync(
                c => c.Id != input.Id && c.ParentId == input.ParentId && c.Name == name,
                ct
            )
        )
        {
            return Failed("MasterData.NameTaken", name);
        }

        var category = input.Id == 0
            ? db.Categories.Add(new Category { Name = name }).Entity
            : await db.Categories.FindAsync([input.Id], ct);

        if (category is null)
        {
            return Failed("MasterData.NotFound");
        }

        category.Name = name;
        category.ParentId = input.ParentId;
        category.Kind = input.Kind;

        await db.SaveChangesAsync(ct);

        return MasterDataResult.Ok;
    }

    public async Task<MasterDataResult> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var usage = await db
            .Categories.Where(c => c.Id == id)
            .Select(c => new
            {
                Children = c.Children.Count,
                Products = db.Products.Count(p => p.CategoryId == c.Id),
                Assets = db.Assets.Count(a => a.CategoryId == c.Id),
            })
            .FirstOrDefaultAsync(ct);

        if (usage is null)
        {
            return Failed("MasterData.NotFound");
        }

        if (usage.Children > 0)
        {
            return Failed("MasterData.HasChildren");
        }

        if (usage.Products > 0 || usage.Assets > 0)
        {
            return Failed("MasterData.CategoryInUse", usage.Products, usage.Assets);
        }

        await db.Categories.Where(c => c.Id == id).ExecuteDeleteAsync(ct);

        return MasterDataResult.Ok;
    }

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

    public async Task<MasterDataResult> SaveStoreAsync(Store input, CancellationToken ct = default)
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
        store.Chain = Blank(input.Chain);
        store.Address = Blank(input.Address);
        store.IsOnline = input.IsOnline;
        store.TaxId = Blank(input.TaxId);

        await db.SaveChangesAsync(ct);

        return MasterDataResult.Ok;
    }

    public async Task<MasterDataResult> DeleteStoreAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var purchases = await db.Purchases.CountAsync(p => p.StoreId == id, ct);

        if (purchases > 0)
        {
            return Failed("MasterData.StoreInUse", purchases);
        }

        // Store-specific aliases go with the store (ON DELETE CASCADE).
        await db.Stores.Where(s => s.Id == id).ExecuteDeleteAsync(ct);

        return MasterDataResult.Ok;
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

    public async Task<MasterDataResult> SaveShoppingListAsync(
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

        return MasterDataResult.Ok;
    }

    public async Task<MasterDataResult> SetDefaultShoppingListAsync(
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

        return MasterDataResult.Ok;
    }

    public async Task<MasterDataResult> SetShoppingListArchivedAsync(
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

        return MasterDataResult.Ok;
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

    private static async Task<bool> CanAttachAsync<T>(
        IQueryable<T> nodes,
        int nodeId,
        int? parentId,
        CancellationToken ct
    )
        where T : class
    {
        if (parentId is null)
        {
            return true;
        }

        var edges = await nodes
            .Select(n => new
            {
                Id = EF.Property<int>(n, "Id"),
                ParentId = EF.Property<int?>(n, "ParentId"),
            })
            .Where(n => n.Id == parentId || n.ParentId == nodeId)
            .ToListAsync(ct);

        var parent = edges.FirstOrDefault(e => e.Id == parentId);
        var hasChildren = nodeId != 0 && edges.Any(e => e.ParentId == nodeId);

        return parent is not null
            && TwoLevelTree.CanAttach(nodeId, hasChildren, parentId, parent.ParentId);
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private MasterDataResult Failed(string key, params object[] arguments) =>
        MasterDataResult.Failed(localizer[key, arguments]);
}
