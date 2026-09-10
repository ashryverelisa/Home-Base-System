using System.Globalization;

namespace HomeBase.Localization;

public static class SupportedCultures
{
    public const string DefaultName = "de-DE";

    public static readonly IReadOnlyList<string> Names = ["de-DE", "en-US"];

    public static readonly IReadOnlyList<CultureInfo> All =
    [
        .. Names.Select(CultureInfo.GetCultureInfo),
    ];

    public static CultureInfo Default => All[0];

    public static bool IsSupported(string? name) =>
        name is not null && Names.Contains(name, StringComparer.OrdinalIgnoreCase);
}
