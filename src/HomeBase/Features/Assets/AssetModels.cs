namespace HomeBase.Features.Assets;

public sealed record AssetFilter(
    string? Term = null,
    bool IncludeRetired = false,
    bool WarrantyEndingSoon = false,
    bool ServiceDue = false
);

public sealed record StoredDocument(string RelativePath, string ContentType);
