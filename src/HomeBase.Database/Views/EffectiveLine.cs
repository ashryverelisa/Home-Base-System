namespace HomeBase.Database.Views;

public class EffectiveLine
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public int? ProductId { get; set; }
    public decimal? QuantityBase { get; set; }
    public bool IsPromo { get; set; }
    public decimal EffectiveTotal { get; set; }
    public decimal? EffectivePricePerBaseUnit { get; set; }
}
