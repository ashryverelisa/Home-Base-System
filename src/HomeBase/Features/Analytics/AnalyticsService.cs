using HomeBase.Database;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Analytics;

public sealed class AnalyticsService(
    IDbContextFactory<HomeBaseDbContext> factory,
    TimeProvider time
) : IAnalyticsService
{
    public async Task<IReadOnlyList<MonthlySpendRow>> GetMonthlySpendAsync(
        int months = 12,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = FirstOfMonth(months);

        var totals = await db
            .MonthlySpend.Where(m => m.Month >= from)
            .GroupBy(m => m.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(m => m.Total) })
            .OrderByDescending(x => x.Month)
            .ToListAsync(ct);

        return [.. totals.Select(t => new MonthlySpendRow(t.Month, t.Total))];
    }

    public async Task<IReadOnlyList<StoreSpendRow>> GetStoreSpendAsync(
        int months = 12,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = FirstOfMonth(months);

        var totals = await db
            .MonthlySpend.Where(m => m.Month >= from)
            .GroupBy(m => m.StoreId)
            .Select(g => new { StoreId = g.Key, Total = g.Sum(m => m.Total) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        var names = await db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        return
        [
            .. totals.Select(t => new StoreSpendRow(
                t.StoreId,
                t.StoreId is { } id ? names.GetValueOrDefault(id, string.Empty) : string.Empty,
                t.Total
            )),
        ];
    }

    public async Task<decimal> GetDepositBalanceAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db.DepositBalance.Select(d => d.OpenDeposit).FirstOrDefaultAsync(ct) ?? 0m;
    }

    public async Task<IReadOnlyList<ProductPriceRow>> GetProductPricesAsync(
        int take = 20,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var spend = await db
            .EffectiveLines.Where(l => l.ProductId != null)
            .GroupBy(l => l.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(l => l.EffectiveTotal) })
            .OrderByDescending(x => x.Total)
            .Take(take)
            .ToListAsync(ct);

        if (spend.Count == 0)
        {
            return [];
        }

        var productIds = spend.ConvertAll(x => x.ProductId);

        var products = await db
            .Products.Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.BaseUnit,
            })
            .ToDictionaryAsync(p => p.Id, ct);

        var points = await db
            .PriceHistory.Where(p => productIds.Contains(p.ProductId) && !p.IsPromo)
            .ToListAsync(ct);

        var latest = points
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.MaxBy(p => p.Day)!);

        return
        [
            .. spend
                .Where(x => products.ContainsKey(x.ProductId))
                .Select(x =>
                {
                    var product = products[x.ProductId];
                    var point = latest.GetValueOrDefault(x.ProductId);

                    return new ProductPriceRow(
                        x.ProductId,
                        product.Name,
                        product.BaseUnit,
                        x.Total,
                        point?.PricePerBaseUnit,
                        point?.PrevPrice,
                        point?.Day
                    );
                }),
        ];
    }

    public async Task<IReadOnlyList<PriceTrendPoint>> GetPriceTrendAsync(
        int productId,
        int months = 12,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = DateOnly.FromDateTime(FirstOfMonth(months).UtcDateTime);

        var points = await db
            .PriceHistory.Where(p =>
                p.ProductId == productId
                && !p.IsPromo
                && p.Day >= from
                && p.PricePerBaseUnit != null
            )
            .Select(p => new { p.Day, Price = p.PricePerBaseUnit!.Value })
            .ToListAsync(ct);

        var observed = points
            .GroupBy(p => new DateTimeOffset(
                new DateTime(p.Day.Year, p.Day.Month, 1),
                TimeSpan.Zero
            ))
            .ToDictionary(g => g.Key, g => g.Average(p => p.Price));

        return CarryForward(observed);
    }

    internal static List<PriceTrendPoint> CarryForward(Dictionary<DateTimeOffset, decimal> observed)
    {
        if (observed.Count == 0)
        {
            return [];
        }

        var month = observed.Keys.Min();
        var last = observed.Keys.Max();
        var price = observed[month];
        var trend = new List<PriceTrendPoint>();

        while (month <= last)
        {
            if (observed.TryGetValue(month, out var observedPrice))
            {
                price = observedPrice;
            }

            trend.Add(new PriceTrendPoint(month, price));
            month = month.AddMonths(1);
        }

        return trend;
    }

    public async Task<IReadOnlyList<StorePriceRow>> GetStoreComparisonAsync(
        int productId,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .BestStorePrices.Where(b => b.ProductId == productId)
            .OrderBy(b => b.AveragePrice)
            .ToListAsync(ct);

        var names = await db.Stores.ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        return
        [
            .. rows.Select(r => new StorePriceRow(
                r.StoreId,
                r.StoreId is { } id ? names.GetValueOrDefault(id, string.Empty) : string.Empty,
                r.AveragePrice,
                r.BestPrice,
                r.Purchases,
                r.LastSeen
            )),
        ];
    }

    public async Task<IReadOnlyList<WasteRow>> GetWasteAsync(
        int months = 12,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = FirstOfMonth(months);

        var rows = await db
            .WasteCosts.Where(w => w.Month >= from)
            .GroupBy(w => w.Month)
            .Select(g => new
            {
                Month = g.Key,
                Cost = g.Sum(w => w.Cost),
                QuantityBase = g.Sum(w => w.QuantityBase),
            })
            .OrderByDescending(x => x.Month)
            .ToListAsync(ct);

        return [.. rows.Select(r => new WasteRow(r.Month, r.Cost, r.QuantityBase))];
    }

    public async Task<IReadOnlyList<WasteProductRow>> GetWastedProductsAsync(
        int months = 12,
        int take = 5,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = FirstOfMonth(months);

        var rows = await db
            .WasteCosts.Where(w => w.Month >= from)
            .GroupBy(w => w.ProductId)
            .Select(g => new { ProductId = g.Key, Cost = g.Sum(w => w.Cost) })
            .OrderByDescending(x => x.Cost)
            .Take(take)
            .ToListAsync(ct);

        var ids = rows.ConvertAll(r => r.ProductId);
        var names = await db
            .Products.Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return
        [
            .. rows.Select(r => new WasteProductRow(
                r.ProductId,
                names.GetValueOrDefault(r.ProductId, string.Empty),
                r.Cost
            )),
        ];
    }

    public async Task<IReadOnlyList<BasketIndexRow>> GetBasketIndexAsync(
        int months = 24,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = FirstOfMonth(months);

        return await db
            .BasketIndex.Where(b => b.Month >= from && b.IndexValue != null)
            .OrderBy(b => b.Month)
            .Select(b => new BasketIndexRow(b.Month, b.IndexValue!.Value, b.Products))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PromoSavingRow>> GetPromoSavingsAsync(
        int months = 12,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var from = FirstOfMonth(months);

        var rows = await db
            .PromoSavings.Where(p => p.Month >= from)
            .GroupBy(p => p.Month)
            .Select(g => new
            {
                Month = g.Key,
                Discount = g.Sum(p => p.DiscountTotal),
                Saved = g.Sum(p => p.SavedAgainstNormal),
            })
            .OrderByDescending(x => x.Month)
            .ToListAsync(ct);

        return [.. rows.Select(r => new PromoSavingRow(r.Month, r.Discount, r.Saved ?? 0m))];
    }

    public async Task<IReadOnlyList<ReachRow>> GetReachAsync(
        int take = 10,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var rows = await db
            .ReachDays.Where(r => r.DaysLeft != null && r.PerWeek != null)
            .OrderBy(r => r.DaysLeft)
            .Take(take)
            .ToListAsync(ct);

        var ids = rows.ConvertAll(r => r.ProductId);

        var products = await db
            .Products.Where(p => ids.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.BaseUnit,
            })
            .ToDictionaryAsync(p => p.Id, ct);

        return
        [
            .. rows.Where(r => products.ContainsKey(r.ProductId))
                .Select(r => new ReachRow(
                    r.ProductId,
                    products[r.ProductId].Name,
                    products[r.ProductId].BaseUnit,
                    r.StockBase,
                    r.PerWeek!.Value,
                    r.DaysLeft!.Value
                )),
        ];
    }

    private DateTimeOffset FirstOfMonth(int monthsBack)
    {
        var today = time.GetUtcNow();

        return new DateTimeOffset(
            new DateTime(today.Year, today.Month, 1),
            TimeSpan.Zero
        ).AddMonths(-(monthsBack - 1));
    }
}
