using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Inventory;

public sealed class InventoryService(IDbContextFactory<HomeBaseDbContext> factory)
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

        if (withinDays is { } days)
        {
            lots = lots.BestBeforeUntil(DateOnly.FromDateTime(DateTime.Today).AddDays(days));
        }

        return await lots.FirstExpiredFirstOut().Select(StockLotView.Projection).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LowStockRow>> GetLowStockAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var products = await db
            .Products.WithMinimumStock()
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.BaseUnit,
                Minimum = p.MinStockBase!.Value,
            })
            .ToListAsync(ct);

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return
        [
            .. products
                .Select(p => new LowStockRow(
                    p.Id,
                    p.Name,
                    p.BaseUnit,
                    totals.GetValueOrDefault(p.Id),
                    p.Minimum
                ))
                .Where(r => r.StockBase < r.MinStockBase)
                .OrderBy(r => r.Name),
        ];
    }

    public async Task<long> BookInAsync(BookInRequest request, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.QuantityBase);

        await using var db = await factory.CreateDbContextAsync(ct);

        var lot = new StockLot
        {
            ProductId = request.ProductId,
            LocationId = request.LocationId,
            QuantityBase = request.QuantityBase,
            BestBefore = request.BestBefore,
            PurchaseItemId = request.PurchaseItemId,
        };

        db.StockLots.Add(lot);
        db.StockMovements.Add(
            new StockMovement
            {
                Lot = lot,
                ProductId = request.ProductId,
                Type = StockMovementType.Purchase,
                QuantityDelta = request.QuantityBase,
                Note = request.Note,
            }
        );

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
            await DeductAsync(db, lotId, lot.ProductId, taken, type, reason, note, ct);
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityBase);

        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var lots = await db
            .StockLots.InStock()
            .ForProduct(productId)
            .FirstExpiredFirstOut()
            .Select(l => new { l.Id, l.QuantityBase })
            .ToListAsync(ct);

        var remaining = quantityBase;

        foreach (var lot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var taken = Math.Min(remaining, lot.QuantityBase);
            await DeductAsync(
                db,
                lot.Id,
                productId,
                taken,
                type,
                reason,
                note,
                ct,
                mealPlanEntryId
            );
            remaining -= taken;
        }

        await transaction.CommitAsync(ct);

        return new StockChangeResult(quantityBase - remaining, remaining);
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

    public async Task MoveLotAsync(long lotId, int? locationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lot =
            await db.StockLots.FindAsync([lotId], ct)
            ?? throw new InvalidOperationException($"Stock lot {lotId} does not exist.");

        if (lot.LocationId == locationId)
        {
            return;
        }

        lot.LocationId = locationId;

        db.StockMovements.Add(
            new StockMovement
            {
                LotId = lotId,
                ProductId = lot.ProductId,
                Type = StockMovementType.Move,
                QuantityDelta = 0,
                Reason = "Umgelagert",
            }
        );

        await db.SaveChangesAsync(ct);
    }

    public async Task OpenLotAsync(long lotId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.StockLots.MarkOpenedAsync(lotId, DateTimeOffset.UtcNow, ct);
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

    private static async Task DeductAsync(
        HomeBaseDbContext db,
        long lotId,
        int productId,
        decimal quantityBase,
        StockMovementType type,
        string? reason,
        string? note,
        CancellationToken ct,
        long? mealPlanEntryId = null
    )
    {
        if (!await db.StockLots.TryDeductAsync(lotId, quantityBase, ct))
        {
            throw new InvalidOperationException(
                $"Stock lot {lotId} no longer holds {quantityBase}; it changed concurrently."
            );
        }

        db.StockMovements.Add(
            new StockMovement
            {
                LotId = lotId,
                ProductId = productId,
                Type = type,
                QuantityDelta = -quantityBase,
                Reason = reason,
                Note = note,
                MealPlanEntryId = mealPlanEntryId,
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
