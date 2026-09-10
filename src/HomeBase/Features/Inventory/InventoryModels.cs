namespace HomeBase.Features.Inventory;

public sealed record BookInRequest(
    int ProductId,
    decimal QuantityBase,
    int? LocationId,
    DateOnly? BestBefore,
    long? PurchaseItemId = null,
    string? Note = null
);

public sealed record StockChangeResult(decimal Applied, decimal Shortfall)
{
    public bool IsComplete => Shortfall == 0;
}
