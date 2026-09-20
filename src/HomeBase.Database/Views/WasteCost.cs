namespace HomeBase.Database.Views;

public class WasteCost
{
    public DateTimeOffset Month { get; set; }
    public int ProductId { get; set; }
    public int? CategoryId { get; set; }
    public decimal QuantityBase { get; set; }
    public decimal Cost { get; set; }
}
