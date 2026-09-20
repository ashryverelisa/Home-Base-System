namespace HomeBase.Database.Views;

public class BestStorePrice
{
    public int ProductId { get; set; }
    public int? StoreId { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal? BestPrice { get; set; }
    public int Purchases { get; set; }
    public DateTimeOffset? LastSeen { get; set; }
}
