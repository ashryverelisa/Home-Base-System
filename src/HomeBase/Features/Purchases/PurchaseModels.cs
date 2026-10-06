using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;

namespace HomeBase.Features.Purchases;

public sealed class PurchaseDraft
{
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal? ReceiptTotal { get; set; }
    public bool BookIntoStock { get; set; } = true;
    public List<PurchaseDraftLine> Lines { get; } = [];

    public decimal LineSum => Lines.Sum(l => l.SignedTotal);

    public IReadOnlyList<int> ItemLineIndexes =>
        [.. Enumerable.Range(0, Lines.Count).Where(index => Lines[index].IsItem)];

    public void RemoveLine(PurchaseDraftLine line)
    {
        var index = Lines.IndexOf(line);

        if (index < 0)
        {
            return;
        }

        Lines.RemoveAt(index);

        foreach (var other in Lines)
        {
            other.ParentIndex = other.ParentIndex switch
            {
                { } parent when parent == index => null,
                { } parent when parent > index => parent - 1,
                var parent => parent,
            };
        }
    }
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

    public void ChangeType(PurchaseLineType type)
    {
        LineType = type;

        if (type != PurchaseLineType.Item)
        {
            ProductId = null;
            ProductName = null;
            BaseUnit = null;
            QuantityBase = null;
            IsPromo = false;
            BestBefore = null;
        }

        if (type != PurchaseLineType.Discount)
        {
            ParentIndex = null;
        }
    }

    public void SelectProduct(ProductRow? product)
    {
        ProductId = product?.Id;
        ProductName = product?.Name;
        BaseUnit = product?.BaseUnit;
        Quantity = 1m;
        QuantityBase = product?.PackageSize;
    }

    public void ChangeQuantity(decimal quantity, decimal? packageSize)
    {
        Quantity = quantity;

        if (packageSize is { } size)
        {
            QuantityBase = quantity * size;
        }
    }

    /// <returns>The resource key of the validation error, or null when the line can be added.</returns>
    public string? Validate()
    {
        if (IsItem && ProductId is null)
        {
            return "Purchases.ProductRequired";
        }

        if (!IsItem && string.IsNullOrWhiteSpace(RawText))
        {
            return "Purchases.TextRequired";
        }

        return LineTotal == 0 ? "Purchases.AmountRequired" : null;
    }
}
