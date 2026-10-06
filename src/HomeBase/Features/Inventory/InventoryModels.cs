using HomeBase.Database.Enums;

namespace HomeBase.Features.Inventory;

public sealed record LotMove(
    long LotId,
    int? LocationId,
    bool ReplaceBestBefore = false,
    DateOnly? BestBefore = null
);

public sealed record BookInRequest(
    int ProductId,
    decimal QuantityBase,
    int? LocationId,
    DateOnly? BestBefore,
    long? PurchaseItemId = null,
    string? Note = null
);

public sealed record LowStockRow(
    int ProductId,
    string Name,
    BaseUnit BaseUnit,
    decimal StockBase,
    decimal MinStockBase
);

public sealed record MinimumStock(
    int ProductId,
    string Name,
    BaseUnit BaseUnit,
    decimal MinStockBase
);

public sealed record StockChangeResult(decimal Applied, decimal Shortfall)
{
    public bool IsComplete => Shortfall == 0;
}

public readonly record struct LotQuantity(long LotId, decimal QuantityBase);

public readonly record struct LotTake(long LotId, decimal QuantityBase);

public sealed record FefoAllocation(
    IReadOnlyList<LotTake> Takes,
    decimal Applied,
    decimal Shortfall
)
{
    public StockChangeResult ToResult() => new(Applied, Shortfall);
}
