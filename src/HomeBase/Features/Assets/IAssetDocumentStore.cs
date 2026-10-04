namespace HomeBase.Features.Assets;

public interface IAssetDocumentStore
{
    Task<StoredDocument> SaveAsync(
        int assetId,
        string fileName,
        Stream content,
        CancellationToken ct = default
    );

    string? Resolve(string relativePath);

    void Delete(string relativePath);
}
