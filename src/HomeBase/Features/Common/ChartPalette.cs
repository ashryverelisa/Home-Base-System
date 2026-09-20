namespace HomeBase.Features.Common;

public static class ChartPalette
{
    private static readonly string[] LightSlots = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100"];

    private static readonly string[] DarkSlots = ["#3987e5", "#d95926", "#199e70", "#c98500"];

    public static string[] For(bool darkMode) => darkMode ? DarkSlots : LightSlots;

    public static string[] Slot(bool darkMode, int slot) => [For(darkMode)[slot]];
}
