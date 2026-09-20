using HomeBase.Database.Enums;

namespace HomeBase.Features.Analytics;

public sealed record MonthlySpendRow(DateTimeOffset Month, decimal Total);

public sealed record StoreSpendRow(int? StoreId, string StoreName, decimal Total);

public sealed record ProductPriceRow(
    int ProductId,
    string Name,
    BaseUnit BaseUnit,
    decimal TotalSpend,
    decimal? LastPrice,
    decimal? PrevPrice,
    DateOnly? LastDay
)
{
    public decimal? Change => LastPrice is { } last && PrevPrice is { } prev ? last - prev : null;

    public bool IsCheaper => Change is < 0;

    public bool IsPricier => Change is > 0;
}

public sealed record PriceTrendPoint(DateTimeOffset Month, decimal Price);

public sealed record StorePriceRow(
    int? StoreId,
    string StoreName,
    decimal? AveragePrice,
    decimal? BestPrice,
    int Purchases,
    DateTimeOffset? LastSeen
);

public sealed record WasteRow(DateTimeOffset Month, decimal Cost, decimal QuantityBase);

public sealed record WasteProductRow(int ProductId, string Name, decimal Cost);

public sealed record BasketIndexRow(DateTimeOffset Month, decimal Index, int Products);

public sealed record PromoSavingRow(DateTimeOffset Month, decimal Discount, decimal Saved)
{
    public decimal Total => Discount + Saved;
}

public sealed record ReachRow(
    int ProductId,
    string Name,
    BaseUnit BaseUnit,
    decimal StockBase,
    decimal PerWeek,
    decimal DaysLeft
)
{
    public bool RunsOutSoon => DaysLeft <= 7;
}
