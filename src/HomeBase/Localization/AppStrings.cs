using System.Globalization;
using System.Resources;

namespace HomeBase.Localization;

public sealed class AppStrings
{
    private static readonly ResourceManager Resources = new(
        "HomeBase.Localization.AppStrings",
        typeof(AppStrings).Assembly
    );

    public static string Get(string name) =>
        Resources.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    public static string Format(string name, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
}
