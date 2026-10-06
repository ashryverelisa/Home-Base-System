using System.Globalization;
using System.Text.RegularExpressions;
using HomeBase.Database;
using HomeBase.Database.Enums;
using HomeBase.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.Recipes;

public sealed record ParsedIngredient(string Name, decimal? Quantity, string? Unit);

// Splits "250 g Mehl" into quantity, unit and name. Used on import and again when an
// ingredient that needed review gets its product.
public static partial class IngredientParser
{
    [GeneratedRegex(
        @"^\s*(?<qty>\d+(?:[.,]\d+)?)?\s*(?<unit>kg|g|l|ml|el|tl|stk|stück|pcs|prise|packung)?\.?\s+(?<name>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    )]
    private static partial Regex LinePattern { get; }

    // A structured name wins over the text; explicit quantity and unit win over parsed ones.
    public static ParsedIngredient Parse(
        string? text,
        decimal? quantity = null,
        string? unit = null,
        string? name = null
    )
    {
        if (name is { Length: > 0 })
        {
            return new ParsedIngredient(name.Trim(), quantity, unit?.Trim());
        }

        if (text is not { Length: > 0 })
        {
            return new ParsedIngredient(string.Empty, null, null);
        }

        var match = LinePattern.Match(text);

        if (!match.Success)
        {
            return new ParsedIngredient(text.Trim(), quantity, unit?.Trim());
        }

        if (
            quantity is null
            && match.Groups["qty"].Success
            && decimal.TryParse(
                match.Groups["qty"].Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsedQuantity
            )
        )
        {
            quantity = parsedQuantity;
        }

        var parsedUnit = match.Groups["unit"].Success
            ? match.Groups["unit"].Value
            : unit?.Trim();

        return new ParsedIngredient(match.Groups["name"].Value.Trim(), quantity, parsedUnit);
    }

    public static decimal? ToBaseQuantity(
        decimal? quantity,
        string? unit,
        BaseUnit baseUnit,
        decimal? pieceWeightBase
    )
    {
        if (quantity is not { } value || value <= 0)
        {
            return null;
        }

        if (UnitConversion.ToBase(value, unit, baseUnit) is { } converted)
        {
            return converted;
        }

        if (!IsCount(unit))
        {
            return null;
        }

        return baseUnit switch
        {
            BaseUnit.Piece => value,
            BaseUnit.Gram when pieceWeightBase is { } weight => value * weight,
            _ => null,
        };
    }

    public static async Task<decimal?> ToBaseQuantityAsync(
        HomeBaseDbContext db,
        int productId,
        decimal? quantity,
        string? unit,
        CancellationToken ct
    )
    {
        if (quantity is not > 0)
        {
            return null;
        }

        var product = await db
            .Products.Where(p => p.Id == productId)
            .Select(p => new { p.BaseUnit, p.PieceWeightBase })
            .FirstOrDefaultAsync(ct);

        return product is null
            ? null
            : ToBaseQuantity(quantity, unit, product.BaseUnit, product.PieceWeightBase);
    }

    private static bool IsCount(string? unit) =>
        unit?.Trim().ToLowerInvariant() is null or "" or "stk" or "stück" or "pcs" or "packung";
}
