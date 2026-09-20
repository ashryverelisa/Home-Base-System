namespace HomeBase.Database.Views;

public class ReachDays
{
    public int ProductId { get; set; }
    public decimal StockBase { get; set; }
    public decimal? PerWeek { get; set; }
    public decimal? DaysLeft { get; set; }
}
