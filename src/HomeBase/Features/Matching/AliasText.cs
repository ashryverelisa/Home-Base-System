using System.Text.RegularExpressions;

namespace HomeBase.Features.Matching;

public static partial class AliasText
{
    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex Separators { get; }

    [GeneratedRegex(@"-\s*\d+(?:[.,]\d+)?\s*%|%\s*(?:RABATT|NACHLASS)")]
    private static partial Regex DiscountPercentage { get; }

    public static string Normalize(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? string.Empty
            : Separators.Replace(raw.Trim().ToLowerInvariant(), " ").Trim();

    public static bool LooksPromotional(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.ToUpperInvariant();

        return PromoKeywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal))
            || DiscountPercentage.IsMatch(text);
    }

    private static readonly string[] PromoKeywords = ["RABATT", "AKTION", "ANGEBOT", "COUPON"];
}
