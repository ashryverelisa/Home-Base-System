using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Purchases;

public sealed record ReviewLineRow(
    long Id,
    int LineNo,
    PurchaseLineType LineType,
    string? RawText,
    string? Gtin,
    int? ProductId,
    string? ProductName,
    int? SuggestedProductId,
    string? SuggestedProductName,
    MatchStatus MatchStatus,
    float? MatchConfidence,
    decimal Quantity,
    decimal? QuantityBase,
    decimal LineTotal,
    decimal? PricePerBaseUnit,
    bool IsPromo,
    BaseUnit? BaseUnit
)
{
    public static readonly Expression<Func<PurchaseItem, ReviewLineRow>> Projection =
        item => new ReviewLineRow(
            item.Id,
            item.LineNo,
            item.LineType,
            item.RawText,
            item.Gtin,
            item.ProductId,
            item.Product!.Name,
            item.SuggestedProductId,
            item.SuggestedProduct!.Name,
            item.MatchStatus,
            item.MatchConfidence,
            item.Quantity,
            item.QuantityBase,
            item.LineTotal,
            item.PricePerBaseUnit,
            item.IsPromo,
            item.Product!.BaseUnit
        );

    public bool PromoHint { get; init; }

    public bool NeedsAssignment => LineType == PurchaseLineType.Item && ProductId is null;

    public string Label => ProductName ?? RawText ?? string.Empty;
}
