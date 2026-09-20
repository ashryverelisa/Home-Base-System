using Microsoft.AspNetCore.StaticFiles;

namespace HomeBase.Features.Assets;

public sealed class AssetDocumentStore
{
    public const string ConfigurationKey = "Assets:DocumentPath";
    public const long MaximumFileSize = 20L * 1024 * 1024;

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private readonly string _root;

    public AssetDocumentStore(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[ConfigurationKey];

        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "assets")
            : Path.GetFullPath(configured);
    }

    public async Task<StoredDocument> SaveAsync(
        int assetId,
        string fileName,
        Stream content,
        CancellationToken ct = default
    )
    {
        var folder = Path.Combine(_root, assetId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(fileName);
        var stored = $"{Guid.NewGuid():N}{extension}";
        var absolute = Path.Combine(folder, stored);

        await using (var target = File.Create(absolute))
        {
            await content.CopyToAsync(target, ct);
        }

        return new StoredDocument(
            Path.GetRelativePath(_root, absolute).Replace('\\', '/'),
            ContentTypeOf(stored)
        );
    }

    public string? Resolve(string relativePath)
    {
        var absolute = Path.GetFullPath(Path.Combine(_root, relativePath));

        return absolute.StartsWith(_root, StringComparison.Ordinal) && File.Exists(absolute)
            ? absolute
            : null;
    }

    public void Delete(string relativePath)
    {
        if (Resolve(relativePath) is { } absolute)
        {
            File.Delete(absolute);
        }
    }

    public static string ContentTypeOf(string fileName) =>
        ContentTypes.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";
}
