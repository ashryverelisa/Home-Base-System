using MudBlazor;

namespace HomeBase.Components.Layout;

public static class AppTheme
{
    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1b6ec2",
            Secondary = "#6c757d",
            AppbarBackground = "#1b6ec2",
            AppbarText = "#ffffff",
            Background = "#f7f8fa",
            DrawerBackground = "#ffffff",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#6ba8e5",
            Secondary = "#9aa4ae",
            AppbarBackground = "#1a1f26",
            Background = "#12161c",
            Surface = "#1a1f26",
            DrawerBackground = "#1a1f26",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "250px",
        },
    };
}
