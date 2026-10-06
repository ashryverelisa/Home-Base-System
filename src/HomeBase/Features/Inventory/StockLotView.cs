using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Features.Common;

namespace HomeBase.Features.Inventory;

public sealed record StockLotView(
    long LotId,
    int ProductId,
    string ProductName,
    string? Brand,
    BaseUnit BaseUnit,
    decimal QuantityBase,
    DateOnly? BestBefore,
    DateTimeOffset? OpenedAt,
    int? LocationId,
    string? LocationName,
    StorageZone? LocationZone,
    decimal? PricePerBaseUnit
)
{
    public static readonly Expression<Func<StockLot, StockLotView>> Projection =
        lot => new StockLotView(
            lot.Id,
            lot.ProductId,
            lot.Product!.Name,
            lot.Product.Brand,
            lot.Product.BaseUnit,
            lot.QuantityBase,
            lot.BestBefore,
            lot.OpenedAt,
            lot.LocationId,
            lot.Location!.Name,
            lot.Location.Zone,
            lot.PurchaseItem!.PricePerBaseUnit
        );

    public const int ExpiryWarningDays = 3;

    public int? DaysLeft =>
        BestBefore is { } date
            ? date.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber
            : null;

    public bool IsExpired => DaysLeft is < 0;

    public bool ExpiresSoon => !IsExpired && IsDueWithin(ExpiryWarningDays);

    public bool IsDueWithin(int days) =>
        DaysLeft is { } left && left <= Zones.WarningDays(LocationZone, days);

    public bool IsOpened => OpenedAt is not null;

    public decimal? Value => PricePerBaseUnit * QuantityBase;
}