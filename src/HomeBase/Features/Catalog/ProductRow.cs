using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Catalog;

public sealed record ProductRow(
    int Id,
    string Name,
    string? Brand,
    string? CategoryName,
    BaseUnit BaseUnit,
    decimal PackageSize,
    bool IsFood,
    string? Gtin,
    decimal? MinStockBase
)
{
    public static readonly Expression<Func<Product, ProductRow>> Projection =
        product => new ProductRow(
            product.Id,
            product.Name,
            product.Brand,
            product.Category!.Name,
            product.BaseUnit,
            product.PackageSize,
            product.IsFood,
            product.Gtin,
            product.MinStockBase
        );

    public decimal StockBase { get; init; }

    public bool IsBelowMinimum => MinStockBase is { } min && StockBase < min;
}
