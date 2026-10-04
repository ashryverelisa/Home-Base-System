namespace HomeBase.Features.Analytics;

public interface IAnalyticsService
{
    Task<IReadOnlyList<MonthlySpendRow>> GetMonthlySpendAsync(
        int months = 12,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<StoreSpendRow>> GetStoreSpendAsync(
        int months = 12,
        CancellationToken ct = default
    );

    Task<decimal> GetDepositBalanceAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ProductPriceRow>> GetProductPricesAsync(
        int take = 20,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<PriceTrendPoint>> GetPriceTrendAsync(
        int productId,
        int months = 12,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<StorePriceRow>> GetStoreComparisonAsync(
        int productId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<WasteRow>> GetWasteAsync(int months = 12, CancellationToken ct = default);

    Task<IReadOnlyList<WasteProductRow>> GetWastedProductsAsync(
        int months = 12,
        int take = 5,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<BasketIndexRow>> GetBasketIndexAsync(
        int months = 24,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<PromoSavingRow>> GetPromoSavingsAsync(
        int months = 12,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<ReachRow>> GetReachAsync(int take = 10, CancellationToken ct = default);
}
