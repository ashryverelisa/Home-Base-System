namespace HomeBase.Database.Views;

public class PricePoint
{
    public int ProductId { get; set; }
    public int? StoreId { get; set; }
    public DateOnly Day { get; set; }
    public decimal? PricePerBaseUnit { get; set; }
    public bool IsPromo { get; set; }
    public decimal? PrevPrice { get; set; }
}
