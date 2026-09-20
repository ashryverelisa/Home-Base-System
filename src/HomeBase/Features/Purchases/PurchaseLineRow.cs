using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Purchases;

public sealed record PurchaseLineRow(
    long Id,
    int LineNo,
    PurchaseLineType LineType,
    int? ProductId,
    string? ProductName,
    string? RawText,
    BaseUnit? BaseUnit,
    decimal Quantity,
    decimal? QuantityBase,
    decimal LineTotal,
    decimal? PricePerBaseUnit,
    bool IsPromo
)
{
    public static readonly Expression<Func<PurchaseItem, PurchaseLineRow>> Projection =
        item => new PurchaseLineRow(
            item.Id,
            item.LineNo,
            item.LineType,
            item.ProductId,
            item.Product!.Name,
            item.RawText,
            item.Product!.BaseUnit,
            item.Quantity,
            item.QuantityBase,
            item.LineTotal,
            item.PricePerBaseUnit,
            item.IsPromo
        );

    public string Label => ProductName ?? RawText ?? string.Empty;
}
