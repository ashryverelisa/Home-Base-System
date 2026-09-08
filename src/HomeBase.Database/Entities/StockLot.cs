namespace HomeBase.Database.Entities;

public class StockLot
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? LocationId { get; set; }
    public StorageLocation? Location { get; set; }
    public decimal QuantityBase { get; set; }
    public DateOnly? BestBefore { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public long? PurchaseItemId { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<StockMovement> Movements { get; set; } = [];
}
