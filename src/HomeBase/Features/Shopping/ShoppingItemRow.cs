using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Shopping;

public sealed record ShoppingItemRow(
    long Id,
    int ListId,
    int? ProductId,
    string? ProductName,
    string? Brand,
    string? FreeText,
    BaseUnit? BaseUnit,
    decimal? Quantity,
    string? Unit,
    int Priority,
    decimal? TargetPrice,
    ShoppingListItemStatus Status,
    ShoppingListItemOrigin AddedBy,
    string? Note
)
{
    public static readonly Expression<Func<ShoppingListItem, ShoppingItemRow>> Projection =
        item => new ShoppingItemRow(
            item.Id,
            item.ListId,
            item.ProductId,
            item.Product!.Name,
            item.Product!.Brand,
            item.FreeText,
            item.Product!.BaseUnit,
            item.Quantity,
            item.Unit,
            item.Priority,
            item.TargetPrice,
            item.Status,
            item.AddedBy,
            item.Note
        );

    public decimal? StockBase { get; init; }

    public string Label => ProductName ?? FreeText ?? string.Empty;

    public bool IsOpen => Status == ShoppingListItemStatus.Open;

    public bool IsAutomatic => AddedBy == ShoppingListItemOrigin.AutoRestock;

    public bool IsImportant => Priority > 0;
}
