using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Brand { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string? Gtin { get; set; }
    public BaseUnit BaseUnit { get; set; } = BaseUnit.Piece;
    public decimal PackageSize { get; set; } = 1m;
    public decimal? PieceWeightBase { get; set; }
    public bool IsFood { get; set; }
    public decimal? MinStockBase { get; set; }
    public decimal? TargetStockBase { get; set; }
    public int? DefaultShelfLifeDays { get; set; }
    public int? DefaultLocationId { get; set; }
    public StorageLocation? DefaultLocation { get; set; }
    public string? ImageUrl { get; set; }
    public string? OffId { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ProductAlias> Aliases { get; set; } = [];
}
