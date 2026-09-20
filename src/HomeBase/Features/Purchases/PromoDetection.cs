namespace HomeBase.Features.Purchases;

public static class PromoDetection
{
    public const decimal HintFactor = 0.85m;

    public static IReadOnlyList<ReviewLineRow> Apply(
        IReadOnlyList<ReviewLineRow> rows,
        IReadOnlyDictionary<int, decimal> mediansByProduct
    ) =>
        [
            .. rows.Select(row =>
                IsHint(row, mediansByProduct) ? row with { PromoHint = true } : row
            ),
        ];

    public static bool IsHint(
        ReviewLineRow row,
        IReadOnlyDictionary<int, decimal> mediansByProduct
    ) =>
        row.ProductId is { } productId
        && row.PricePerBaseUnit is { } price
        && mediansByProduct.TryGetValue(productId, out var median)
        && IsHint(price, median);

    public static bool IsHint(decimal pricePerBaseUnit, decimal medianPricePerBaseUnit) =>
        pricePerBaseUnit < medianPricePerBaseUnit * HintFactor;

    public static Dictionary<int, decimal> MediansByProduct(IEnumerable<ProductPrice> history) =>
        history
            .GroupBy(h => h.ProductId)
            .ToDictionary(g => g.Key, g => Median([.. g.Select(h => h.PricePerBaseUnit)]));

    public static decimal Median(List<decimal> values)
    {
        values.Sort();

        var middle = values.Count / 2;

        return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2m;
    }
}

public readonly record struct ProductPrice(int ProductId, decimal PricePerBaseUnit);
