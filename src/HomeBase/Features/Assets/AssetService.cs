using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.Assets;

public sealed class AssetService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IAssetDocumentStore documents,
    IStringLocalizer<AppStrings> localizer,
    TimeProvider time
) : IAssetService
{
    public async Task<IReadOnlyList<AssetRow>> SearchAsync(
        AssetFilter? filter = null,
        CancellationToken ct = default
    )
    {
        filter ??= new AssetFilter();

        await using var db = await factory.CreateDbContextAsync(ct);

        var assets = filter.IncludeRetired ? db.Assets : db.Assets.Active();

        if (filter.Term is { Length: > 0 } term)
        {
            assets = assets.MatchingSearch(term.Trim());
        }

        var today = time.Today();

        if (filter.WarrantyEndingSoon)
        {
            assets = assets.WarrantyEndingUntil(today.AddDays(AssetRow.WarrantyWarningDays));
        }

        if (filter.ServiceDue)
        {
            assets = assets.ServiceDueUntil(today);
        }

        var rows = await assets.InDisplayOrder().Select(AssetRow.Projection(today)).ToListAsync(ct);

        var counts = await db.AssetDocuments.DocumentCountsAsync(ct);

        return [.. rows.Select(r => r with { DocumentCount = counts.GetValueOrDefault(r.Id) })];
    }

    public async Task<Asset?> FindAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.Assets.FindAsync([id], ct);
    }

    public async Task<SaveResult<int>> SaveAsync(Asset asset, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(asset.Name))
        {
            return SaveResult.Failed<int>(localizer["Assets.NameRequired"]);
        }

        asset.Name = asset.Name.Trim();
        asset.SerialNumber = asset.SerialNumber.TrimToNull();

        await using var db = await factory.CreateDbContextAsync(ct);

        if (asset.Id == 0)
        {
            db.Assets.Add(asset);
        }
        else
        {
            db.Assets.Update(asset);
        }

        await db.SaveChangesAsync(ct);

        return SaveResult.Ok(asset.Id);
    }

    public async Task SetStatusAsync(int id, AssetStatus status, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var asset = await db.Assets.FindAsync([id], ct);

        if (asset is null || asset.Status == status)
        {
            return;
        }

        asset.Status = status;
        asset.DisposedAt = status is AssetStatus.Sold or AssetStatus.Disposed
            ? time.Today()
            : null;

        await db.SaveChangesAsync(ct);
    }

    public async Task<DateOnly?> CompleteServiceAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var asset = await db.Assets.FindAsync([id], ct);

        if (asset is null)
        {
            return null;
        }

        asset.NextServiceAt = asset.ServiceIntervalDays is { } days and > 0
            ? time.Today().AddDays(days)
            : null;

        await db.SaveChangesAsync(ct);

        return asset.NextServiceAt;
    }

    public async Task<IReadOnlyList<AssetDocumentRow>> GetDocumentsAsync(
        int assetId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .AssetDocuments.ForAsset(assetId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(AssetDocumentRow.Projection)
            .ToListAsync(ct);
    }

    public async Task<AssetDocumentRow?> FindDocumentAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .AssetDocuments.Where(d => d.Id == id)
            .Select(AssetDocumentRow.Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddDocumentAsync(
        int assetId,
        AssetDocumentType type,
        string fileName,
        Stream content,
        CancellationToken ct = default
    )
    {
        var stored = await documents.SaveAsync(assetId, fileName, content, ct);

        await using var db = await factory.CreateDbContextAsync(ct);

        db.AssetDocuments.Add(
            new AssetDocument
            {
                AssetId = assetId,
                Type = type,
                FilePath = stored.RelativePath,
            }
        );

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDocumentAsync(int id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var document = await db.AssetDocuments.FindAsync([id], ct);

        if (document is null)
        {
            return;
        }

        documents.Delete(document.FilePath);

        db.AssetDocuments.Remove(document);

        await db.SaveChangesAsync(ct);
    }
}
