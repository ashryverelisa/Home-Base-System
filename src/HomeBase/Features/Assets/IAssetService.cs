using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Assets;

public interface IAssetService
{
    Task<IReadOnlyList<AssetRow>> SearchAsync(
        AssetFilter? filter = null,
        CancellationToken ct = default
    );

    Task<Asset?> FindAsync(int id, CancellationToken ct = default);

    Task<AssetSaveResult> SaveAsync(Asset asset, CancellationToken ct = default);

    Task SetStatusAsync(int id, AssetStatus status, CancellationToken ct = default);

    Task<DateOnly?> CompleteServiceAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<AssetDocumentRow>> GetDocumentsAsync(
        int assetId,
        CancellationToken ct = default
    );

    Task<AssetDocumentRow?> FindDocumentAsync(int id, CancellationToken ct = default);

    Task AddDocumentAsync(
        int assetId,
        AssetDocumentType type,
        string fileName,
        Stream content,
        CancellationToken ct = default
    );

    Task DeleteDocumentAsync(int id, CancellationToken ct = default);
}
