namespace HomeBase.Database.Views;

public class MonthlySpend
{
    public DateTimeOffset Month { get; set; }
    public int? StoreId { get; set; }
    public int? CategoryId { get; set; }
    public decimal Total { get; set; }
}
