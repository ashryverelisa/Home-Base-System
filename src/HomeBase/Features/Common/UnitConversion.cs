using HomeBase.Database.Enums;

namespace HomeBase.Features.Common;

// Metric units as they appear on receipts, recipes and Open Food Facts, mapped onto base units.
public static class UnitConversion
{
    public static (BaseUnit Unit, decimal Factor)? Metric(string? unit) =>
        unit?.Trim().ToLowerInvariant() switch
        {
            "g" or "gr" or "gramm" => (BaseUnit.Gram, 1m),
            "kg" => (BaseUnit.Gram, 1000m),
            "ml" => (BaseUnit.Milliliter, 1m),
            "cl" => (BaseUnit.Milliliter, 10m),
            "dl" => (BaseUnit.Milliliter, 100m),
            "l" or "lt" or "liter" or "litre" => (BaseUnit.Milliliter, 1000m),
            _ => null,
        };

    // Null when the unit is not metric or measures something else than the product.
    public static decimal? ToBase(decimal quantity, string? unit, BaseUnit baseUnit) =>
        Metric(unit) is { } metric && metric.Unit == baseUnit ? quantity * metric.Factor : null;
}
