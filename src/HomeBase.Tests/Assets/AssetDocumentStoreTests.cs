using System.Text;
using HomeBase.Features.Assets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HomeBase.Tests.Assets;

public sealed class AssetDocumentStoreTests : IDisposable
{
    private readonly string _workspace = Path.Combine(
        Path.GetTempPath(),
        "homebase-tests",
        Guid.NewGuid().ToString("N")
    );
    private readonly AssetDocumentStore _store;

    public AssetDocumentStoreTests()
    {
        var root = Path.Combine(_workspace, "assets");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [AssetDocumentStore.ConfigurationKey] = root }
            )
            .Build();

        _store = new AssetDocumentStore(configuration, new FakeEnvironment(_workspace));
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace))
        {
            Directory.Delete(_workspace, recursive: true);
        }
    }

    private async Task<StoredDocument> SaveAsync(int assetId, string fileName, string content)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        return await _store.SaveAsync(
            assetId,
            fileName,
            stream,
            TestContext.Current.CancellationToken
        );
    }

    [Fact]
    public async Task SaveAsync_StoresContentInAssetFolderWithRelativeForwardSlashPath()
    {
        var stored = await SaveAsync(7, "Rechnung.pdf", "pdf-bytes");

        Assert.StartsWith("7/", stored.RelativePath, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", stored.RelativePath, StringComparison.Ordinal);
        Assert.DoesNotContain("Rechnung", stored.RelativePath, StringComparison.Ordinal);
        Assert.Equal("application/pdf", stored.ContentType);

        var absolute = Assert.IsType<string>(_store.Resolve(stored.RelativePath));
        Assert.Equal(
            "pdf-bytes",
            await File.ReadAllTextAsync(absolute, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task SaveAsync_SameFileNameTwice_DoesNotOverwrite()
    {
        var first = await SaveAsync(1, "foto.jpg", "a");
        var second = await SaveAsync(1, "foto.jpg", "b");

        Assert.NotEqual(first.RelativePath, second.RelativePath);
    }

    [Fact]
    public void Resolve_MissingFile_ReturnsNull()
    {
        Assert.Null(_store.Resolve("1/does-not-exist.pdf"));
    }

    [Fact]
    public void Resolve_PathOutsideRoot_ReturnsNull()
    {
        Directory.CreateDirectory(_workspace);
        File.WriteAllText(Path.Combine(_workspace, "secret.txt"), "secret");

        Assert.Null(_store.Resolve("../secret.txt"));
    }

    [Fact]
    public async Task Delete_RemovesStoredFile()
    {
        var stored = await SaveAsync(3, "anleitung.txt", "text");

        _store.Delete(stored.RelativePath);

        Assert.Null(_store.Resolve(stored.RelativePath));
    }

    [Fact]
    public void Delete_UnknownFile_DoesNotThrow()
    {
        _store.Delete("9/unknown.txt");
    }

    [Theory]
    [InlineData("scan.pdf", "application/pdf")]
    [InlineData("foto.JPG", "image/jpeg")]
    [InlineData("bild.png", "image/png")]
    [InlineData("datei.unbekannt", "application/octet-stream")]
    [InlineData("ohne-endung", "application/octet-stream")]
    public void ContentTypeOf_UsesExtension(string fileName, string expected)
    {
        Assert.Equal(expected, AssetDocumentStore.ContentTypeOf(fileName));
    }

    private sealed class FakeEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "HomeBase.Tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
