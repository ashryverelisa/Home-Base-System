using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Database.Queries;

public static class AssetQueries
{
    public static IQueryable<Asset> Active(this IQueryable<Asset> assets) =>
        assets.Where(a => a.Status != AssetStatus.Sold && a.Status != AssetStatus.Disposed);

    public static IQueryable<Asset> WithStatus(this IQueryable<Asset> assets, AssetStatus status) =>
        assets.Where(a => a.Status == status);

    public static IQueryable<Asset> MatchingSearch(this IQueryable<Asset> assets, string term) =>
        assets.Where(a =>
            EF.Functions.ILike(a.Name, $"%{term}%")
            || (a.SerialNumber != null && EF.Functions.ILike(a.SerialNumber, $"%{term}%"))
            || (a.Notes != null && EF.Functions.ILike(a.Notes, $"%{term}%"))
        );

    public static IQueryable<Asset> WarrantyEndingUntil(this IQueryable<Asset> assets, DateOnly until) =>
        assets.Where(a => a.WarrantyUntil != null && a.WarrantyUntil <= until);

    public static IQueryable<Asset> ServiceDueUntil(this IQueryable<Asset> assets, DateOnly until) =>
        assets.Where(a => a.NextServiceAt != null && a.NextServiceAt <= until);

    public static IOrderedQueryable<Asset> InDisplayOrder(this IQueryable<Asset> assets) =>
        assets.OrderBy(a => a.Name);

    public static IQueryable<AssetDocument> ForAsset(
        this IQueryable<AssetDocument> documents,
        int assetId
    ) => documents.Where(d => d.AssetId == assetId);

    public static Task<Dictionary<int, int>> DocumentCountsAsync(
        this IQueryable<AssetDocument> documents,
        CancellationToken ct = default
    ) =>
        documents
            .GroupBy(d => d.AssetId)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AssetId, x => x.Count, ct);
}
