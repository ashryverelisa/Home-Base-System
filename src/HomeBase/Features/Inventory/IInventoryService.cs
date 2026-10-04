using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Inventory;

public interface IInventoryService
{
    Task<IReadOnlyList<StockLotView>> GetStockAsync(
        int? locationId = null,
        int? withinDays = null,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<LowStockRow>> GetLowStockAsync(CancellationToken ct = default);

    Task<long> BookInAsync(BookInRequest request, CancellationToken ct = default);

    Task<StockChangeResult> TakeFromLotAsync(
        long lotId,
        decimal quantityBase,
        StockMovementType type,
        string? reason = null,
        string? note = null,
        CancellationToken ct = default
    );

    Task<StockChangeResult> TakeFromProductAsync(
        int productId,
        decimal quantityBase,
        StockMovementType type,
        string? reason = null,
        string? note = null,
        long? mealPlanEntryId = null,
        CancellationToken ct = default
    );

    Task CorrectLotAsync(
        long lotId,
        decimal newQuantityBase,
        string? reason = null,
        CancellationToken ct = default
    );

    Task MoveLotAsync(long lotId, int? locationId, CancellationToken ct = default);

    Task OpenLotAsync(long lotId, CancellationToken ct = default);

    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        int productId,
        int take = 50,
        CancellationToken ct = default
    );
}
