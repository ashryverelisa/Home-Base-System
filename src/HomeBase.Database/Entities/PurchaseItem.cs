using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class PurchaseItem
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public int LineNo { get; set; }
    public PurchaseLineType LineType { get; set; } = PurchaseLineType.Item;
    public long? ParentItemId { get; set; }
    public PurchaseItem? ParentItem { get; set; }
    public List<PurchaseItem> Children { get; set; } = [];
    public string? RawText { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? Gtin { get; set; }
    public decimal Quantity { get; set; }
    public decimal? QuantityBase { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal Discount { get; set; }
    public bool IsPromo { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? PricePerBaseUnit { get; private set; }
    public MatchStatus MatchStatus { get; set; } = MatchStatus.Unmatched;
    public float? MatchConfidence { get; set; }
    public int? SuggestedProductId { get; set; }
    public Product? SuggestedProduct { get; set; }
}
