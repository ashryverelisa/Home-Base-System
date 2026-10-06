using HomeBase.Database.Entities;
using HomeBase.Features.Common;

namespace HomeBase.Features.Purchases;

public interface IPurchaseService
{
    Task<IReadOnlyList<PurchaseRow>> GetPurchasesAsync(int take = 50, CancellationToken ct = default);

    Task<IReadOnlyList<PurchaseRow>> GetPendingAsync(CancellationToken ct = default);

    Task<PurchaseRow?> FindAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<PurchaseLineRow>> GetLinesAsync(
        long purchaseId,
        CancellationToken ct = default
    );

    Task<IReadOnlyList<Store>> GetStoresAsync(CancellationToken ct = default);

    Task<SaveResult<long>> SaveAsync(PurchaseDraft draft, CancellationToken ct = default);

    Task<bool> ConfirmAsync(long purchaseId, CancellationToken ct = default);
}
