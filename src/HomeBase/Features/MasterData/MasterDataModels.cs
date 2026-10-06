using HomeBase.Database.Enums;

namespace HomeBase.Features.MasterData;

public interface ITreeRow
{
    int Id { get; }
    int? ParentId { get; }
    string Name { get; }
}

public sealed record LocationRow(
    int Id,
    string Name,
    int? ParentId,
    StorageZone Zone,
    int ChildCount,
    int LotCount,
    int AssetCount
) : ITreeRow;

public sealed record CategoryRow(
    int Id,
    string Name,
    int? ParentId,
    CategoryKind Kind,
    int ChildCount,
    int ProductCount,
    int AssetCount
) : ITreeRow;

public sealed record StoreRow(
    int Id,
    string Name,
    string? Chain,
    string? Address,
    bool IsOnline,
    string? TaxId,
    int PurchaseCount
);

public sealed record ShoppingListAdminRow(
    int Id,
    string Name,
    ShoppingListKind Kind,
    bool IsDefault,
    int SortOrder,
    bool Archived,
    int OpenCount
);
