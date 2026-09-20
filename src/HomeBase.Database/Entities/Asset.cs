using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class Asset
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? SerialNumber { get; set; }
    public int? LocationId { get; set; }
    public StorageLocation? Location { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InUse;
    public long? PurchaseItemId { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
    public DateOnly? PurchasedAt { get; set; }
    public decimal? PurchasePrice { get; set; }
    public DateOnly? WarrantyUntil { get; set; }
    public decimal? CurrentValue { get; set; }
    public DateOnly? DisposedAt { get; set; }
    public DateOnly? NextServiceAt { get; set; }
    public int? ServiceIntervalDays { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = [];
    public string? Notes { get; set; }
    public List<AssetDocument> Documents { get; set; } = [];
}
