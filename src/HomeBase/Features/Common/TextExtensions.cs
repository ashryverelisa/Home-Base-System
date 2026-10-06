namespace HomeBase.Features.Common;

public static class TextExtensions
{
    public static string? TrimToNull(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
