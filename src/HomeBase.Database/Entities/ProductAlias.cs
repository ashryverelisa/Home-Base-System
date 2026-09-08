using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class ProductAlias
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int? StoreId { get; set; }
    public Store? Store { get; set; }
    public required string RawText { get; set; }
    public required string NormalizedText { get; set; }
    public int TimesSeen { get; set; } = 1;
    public AliasSource Source { get; set; } = AliasSource.Manual;
}
