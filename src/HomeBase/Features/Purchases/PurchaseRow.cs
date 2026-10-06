using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Purchases;

public sealed record PurchaseRow(
    long Id,
    DateTimeOffset PurchasedAt,
    int? StoreId,
    string? StoreName,
    decimal? Total,
    PurchaseSource Source,
    PurchaseStatus Status
)
{
    public static readonly Expression<Func<Purchase, PurchaseRow>> Projection =
        purchase => new PurchaseRow(
            purchase.Id,
            purchase.PurchasedAt,
            purchase.StoreId,
            purchase.Store!.Name,
            purchase.Total,
            purchase.Source,
            purchase.Status
        );

    public int LineCount { get; init; }

    public int UnmatchedCount { get; init; }
}
