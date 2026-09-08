using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class AssetDocument
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }
    public AssetDocumentType Type { get; set; }
    public required string FilePath { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
