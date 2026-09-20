namespace HomeBase.Database.Views;

public class BasketIndexPoint
{
    public DateTimeOffset Month { get; set; }
    public decimal? IndexValue { get; set; }
    public int Products { get; set; }
}
