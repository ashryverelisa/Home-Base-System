namespace HomeBase.Features.Assets;

public sealed record AssetFilter(
    string? Term = null,
    bool IncludeRetired = false,
    bool WarrantyEndingSoon = false,
    bool ServiceDue = false
);

public sealed record AssetSaveResult(bool Succeeded, int AssetId, string? Error)
{
    public static AssetSaveResult Ok(int assetId) => new(true, assetId, null);

    public static AssetSaveResult Failed(string error) => new(false, 0, error);
}

public sealed record StoredDocument(string RelativePath, string ContentType);
