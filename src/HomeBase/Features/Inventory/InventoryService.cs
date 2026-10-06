using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Common;
using HomeBase.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomeBase.Features.Inventory;

public sealed class InventoryService(
    IDbContextFactory<HomeBaseDbContext> factory,
    IStringLocalizer<AppStrings> localizer,
    TimeProvider time
) : IInventoryService
{
    public async Task<IReadOnlyList<StockLotView>> GetStockAsync(
        int? locationId = null,
        int? withinDays = null,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lots = db.StockLots.InStock();

        if (locationId is { } id)
        {
            lots = lots.InLocations(await db.StorageLocations.BranchIdsAsync(id, ct));
        }

        var today = time.Today();

        if (withinDays is { } days)
        {
            lots = lots.DueBy(
                today.AddDays(days),
                today.AddDays(Zones.WarningDays(StorageZone.Freezer, days))
            );
        }

        return await lots
            .FirstExpiredFirstOut()
            .Select(StockLotView.Projection(today))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LowStockRow>> GetLowStockAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var minimums = await db
            .Products.WithMinimumStock()
            .Select(p => new MinimumStock(p.Id, p.Name, p.BaseUnit, p.MinStockBase!.Value))
            .ToListAsync(ct);

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return LowStockRule.Below(minimums, totals);
    }

    public async Task<long> BookInAsync(BookInRequest request, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lot = StockLedger.AddLot(db, request);

        await db.SaveChangesAsync(ct);

        return lot.Id;
    }

    public async Task<StockChangeResult> TakeFromLotAsync(
        long lotId,
        decimal quantityBase,
        StockMovementType type,
        string? reason = null,
        string? note = null,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityBase);

        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var lot =
            await db.StockLots.FindAsync([lotId], ct)
            ?? throw new InvalidOperationException($"Stock lot {lotId} does not exist.");

        var taken = Math.Min(quantityBase, lot.QuantityBase);

        if (taken > 0)
        {
            await StockLedger.DeductAsync(db, lotId, lot.ProductId, taken, type, reason, note, ct);
            await transaction.CommitAsync(ct);
        }

        return new StockChangeResult(taken, quantityBase - taken);
    }

    public async Task<StockChangeResult> TakeFromProductAsync(
        int productId,
        decimal quantityBase,
        StockMovementType type,
        string? reason = null,
        string? note = null,
        long? mealPlanEntryId = null,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var result = await StockLedger.TakeFromProductAsync(
            db,
            productId,
            quantityBase,
            type,
            reason,
            note,
            mealPlanEntryId,
            ct
        );

        await transaction.CommitAsync(ct);

        return result;
    }

    public async Task CorrectLotAsync(
        long lotId,
        decimal newQuantityBase,
        string? reason = null,
        CancellationToken ct = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newQuantityBase);

        await using var db = await factory.CreateDbContextAsync(ct);

        var lot =
            await db.StockLots.FindAsync([lotId], ct)
            ?? throw new InvalidOperationException($"Stock lot {lotId} does not exist.");

        var delta = newQuantityBase - lot.QuantityBase;

        if (delta == 0)
        {
            return;
        }

        lot.QuantityBase = newQuantityBase;
        db.StockMovements.Add(
            new StockMovement
            {
                LotId = lotId,
                ProductId = lot.ProductId,
                Type = StockMovementType.Correction,
                QuantityDelta = delta,
                Reason = reason,
            }
        );

        await db.SaveChangesAsync(ct);
    }

    public async Task MoveLotAsync(LotMove move, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lot =
            await db.StockLots.FindAsync([move.LotId], ct)
            ?? throw new InvalidOperationException($"Stock lot {move.LotId} does not exist.");

        var newDate = move.ReplaceBestBefore && lot.BestBefore != move.BestBefore;

        if (lot.LocationId == move.LocationId && !newDate)
        {
            return;
        }

        lot.LocationId = move.LocationId;

        if (newDate)
        {
            lot.BestBefore = move.BestBefore;
        }

        db.StockMovements.Add(
            new StockMovement
            {
                LotId = move.LotId,
                ProductId = lot.ProductId,
                Type = StockMovementType.Move,
                QuantityDelta = 0,
                Reason = localizer["Stock.MoveReason"].Value,
                Note = newDate ? localizer["Stock.BestBeforeReset"].Value : null,
            }
        );

        await db.SaveChangesAsync(ct);
    }

    public async Task OpenLotAsync(long lotId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.StockLots.MarkOpenedAsync(lotId, time.GetUtcNow(), ct);
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        int productId,
        int take = 50,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.StockMovements.ForProduct(productId).Newest(take).ToListAsync(ct);
    }
}
