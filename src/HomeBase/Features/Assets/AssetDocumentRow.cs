using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Assets;

public sealed record AssetDocumentRow(
    int Id,
    int AssetId,
    AssetDocumentType Type,
    string FilePath,
    DateTimeOffset UploadedAt
)
{
    public static readonly Expression<Func<AssetDocument, AssetDocumentRow>> Projection =
        document => new AssetDocumentRow(
            document.Id,
            document.AssetId,
            document.Type,
            document.FilePath,
            document.UploadedAt
        );

    public string FileName => Path.GetFileName(FilePath);
}
