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
    public static readonly Expression<Func<Asset, AssetRow>> Projection =
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
        );

    public const int WarrantyWarningDays = 60;

    public int DocumentCount { get; init; }

    public int? WarrantyDaysLeft =>
        WarrantyUntil is { } until
            ? until.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber
            : null;

    public bool WarrantyExpired => WarrantyDaysLeft is < 0;

    public bool WarrantyEndingSoon => WarrantyDaysLeft is >= 0 and <= WarrantyWarningDays;

    public bool ServiceDue =>
        NextServiceAt is { } due && due <= DateOnly.FromDateTime(DateTime.Today);
}
