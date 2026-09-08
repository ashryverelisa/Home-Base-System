using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class Purchase
{
    public long Id { get; set; }
    public int? StoreId { get; set; }
    public Store? Store { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
    public decimal? Total { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? PaymentMethod { get; set; }
    public PurchaseSource Source { get; set; } = PurchaseSource.Manual;
    public string? ExternalId { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public string? ReceiptFile { get; set; }
    public string? RawPayload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<PurchaseItem> Items { get; set; } = [];
}
