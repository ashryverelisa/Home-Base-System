using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Inventory;

// Every stock change writes a lot update and its movement together. These work on the caller's
// context, so purchases and cooking can book stock inside their own transaction.
public static class StockLedger
{
    public static StockLot AddLot(HomeBaseDbContext db, BookInRequest request)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.QuantityBase);

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

        return lot;
    }

    public static async Task<StockChangeResult> TakeFromProductAsync(
        HomeBaseDbContext db,
        int productId,
        decimal quantityBase,
        StockMovementType type,
        string? reason,
        string? note,
        long? mealPlanEntryId,
        CancellationToken ct
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantityBase);

        var lots = await db
            .StockLots.InStock()
            .ForProduct(productId)
            .FirstExpiredFirstOut()
            .Select(l => new LotQuantity(l.Id, l.QuantityBase))
            .ToListAsync(ct);

        var allocation = FefoAllocator.Allocate(lots, quantityBase);

        foreach (var take in allocation.Takes)
        {
            await DeductAsync(
                db,
                take.LotId,
                productId,
                take.QuantityBase,
                type,
                reason,
                note,
                ct,
                mealPlanEntryId
            );
        }

        return allocation.ToResult();
    }

    public static async Task DeductAsync(
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
