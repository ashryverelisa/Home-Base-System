using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Assets;

public sealed record AssetRow(
    int Id,
    string Name,
    string? CategoryName,
    string? SerialNumber,
    int? LocationId,
    string? LocationName,
    StorageZone? LocationZone,
    AssetStatus Status,
    DateOnly? PurchasedAt,
    decimal? PurchasePrice,
    decimal? CurrentValue,
    DateOnly? WarrantyUntil,
    DateOnly? NextServiceAt
)
{
    // Warranty and service state are judged against the day the caller reads the list on.
    public static Expression<Func<Asset, AssetRow>> Projection(DateOnly today) =>
        asset => new AssetRow(
            asset.Id,
            asset.Name,
            asset.Category!.Name,
            asset.SerialNumber,
            asset.LocationId,
            asset.Location!.Name,
            asset.Location!.Zone,
            asset.Status,
            asset.PurchasedAt,
            asset.PurchasePrice,
            asset.CurrentValue,
            asset.WarrantyUntil,
            asset.NextServiceAt
        )
        {
            Today = today,
        };

    public const int WarrantyWarningDays = 60;

    public required DateOnly Today { get; init; }

    public int DocumentCount { get; init; }

    public int? WarrantyDaysLeft =>
        WarrantyUntil is { } until
            ? until.DayNumber - Today.DayNumber
            : null;

    public bool WarrantyExpired => WarrantyDaysLeft is < 0;

    public bool WarrantyEndingSoon => WarrantyDaysLeft is >= 0 and <= WarrantyWarningDays;

    public bool ServiceDue =>
        NextServiceAt is { } due && due <= Today;
}
