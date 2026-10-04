namespace HomeBase.Features.Purchases;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewLineRow>> GetLinesAsync(long purchaseId, CancellationToken ct = default);

    Task<bool> AssignAsync(long itemId, int productId, CancellationToken ct = default);

    Task SetPromoAsync(long itemId, bool isPromo, CancellationToken ct = default);

    Task<decimal> GetLineSumAsync(long purchaseId, CancellationToken ct = default);
}
