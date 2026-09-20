namespace HomeBase.Features.Inventory;

public static class FefoAllocator
{
    public static FefoAllocation Allocate(
        IReadOnlyList<LotQuantity> lotsInFefoOrder,
        decimal quantityBase
    )
    {
        List<LotTake> takes = [];
        var remaining = quantityBase;

        foreach (var lot in lotsInFefoOrder)
        {
            if (remaining <= 0)
            {
                break;
            }

            var taken = Math.Min(remaining, lot.QuantityBase);

            if (taken <= 0)
            {
                continue;
            }

            takes.Add(new LotTake(lot.LotId, taken));
            remaining -= taken;
        }

        return new FefoAllocation(takes, quantityBase - remaining, remaining);
    }
}
