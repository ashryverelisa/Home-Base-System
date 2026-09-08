using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class StockMovement
{
    public long Id { get; set; }
    public long? LotId { get; set; }
    public StockLot? Lot { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public StockMovementType Type { get; set; }
    public decimal QuantityDelta { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Reason { get; set; }
    public string? Note { get; set; }
    public long? MealPlanEntryId { get; set; }
    public MealPlanEntry? MealPlanEntry { get; set; }
}
