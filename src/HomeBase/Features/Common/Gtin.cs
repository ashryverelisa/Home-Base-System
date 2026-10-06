using System.Collections.Frozen;

namespace HomeBase.Features.Common;

public static class Gtin
{
    private static readonly FrozenSet<int> ValidLengths = FrozenSet.ToFrozenSet([8, 12, 13, 14]);
    private const int OddPositionWeight = 3;
    private const int EvenPositionWeight = 1;
    private const int Modulus = 10;

    private const char Separator = '-';

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = string.Concat(value.Where(c => !char.IsWhiteSpace(c) && c != Separator));

        return IsValid(digits) ? digits : null;
    }

    public static bool IsValid(string? value)
    {
        if (value is null || !ValidLengths.Contains(value.Length))
        {
            return false;
        }

        if (!value.All(char.IsAsciiDigit))
        {
            return false;
        }

        return CheckDigit(value.AsSpan(0, value.Length - 1)) == DigitValue(value[^1]);
    }

    private static int CheckDigit(ReadOnlySpan<char> payload)
    {
        var sum = 0;

        for (var fromRight = 0; fromRight < payload.Length; fromRight++)
        {
            var weight = fromRight % 2 == 0 ? OddPositionWeight : EvenPositionWeight;
            sum += DigitValue(payload[^(fromRight + 1)]) * weight;
        }

        return (Modulus - sum % Modulus) % Modulus;
    }

    private static int DigitValue(char digit) => digit - '0';
}
