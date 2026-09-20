using HomeBase.Database.Enums;

namespace HomeBase.Features.Purchases;

public sealed class PurchaseDraft
{
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public DateTimeOffset PurchasedAt { get; set; } = DateTimeOffset.Now;
    public string? PaymentMethod { get; set; }
    public decimal? ReceiptTotal { get; set; }
    public bool BookIntoStock { get; set; } = true;
    public List<PurchaseDraftLine> Lines { get; } = [];

    public decimal LineSum => Lines.Sum(l => l.SignedTotal);
}

public sealed class PurchaseDraftLine
{
    public PurchaseLineType LineType { get; set; } = PurchaseLineType.Item;
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public BaseUnit? BaseUnit { get; set; }
    public string? RawText { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal? QuantityBase { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsPromo { get; set; }
    public DateOnly? BestBefore { get; set; }
    public int? ParentIndex { get; set; }

    public bool IsItem => LineType == PurchaseLineType.Item;

    public bool IsCredit =>
        LineType is PurchaseLineType.Discount or PurchaseLineType.DepositReturn;

    public decimal SignedTotal => IsCredit ? -Math.Abs(LineTotal) : Math.Abs(LineTotal);

    public string Label => ProductName ?? RawText ?? string.Empty;
}

public sealed record PurchaseSaveResult(bool Succeeded, long PurchaseId, string? Error)
{
    public static PurchaseSaveResult Ok(long purchaseId) => new(true, purchaseId, null);

    public static PurchaseSaveResult Failed(string error) => new(false, 0, error);
}
