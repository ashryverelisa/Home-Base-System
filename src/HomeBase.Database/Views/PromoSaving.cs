namespace HomeBase.Database.Views;

public class PromoSaving
{
    public DateTimeOffset Month { get; set; }
    public int? StoreId { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal? SavedAgainstNormal { get; set; }
}
