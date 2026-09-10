using MudBlazor;

namespace HomeBase.Components.Layout;

public partial class MainLayout
{
    private MudThemeProvider _themeProvider = null!;
    private bool _drawerOpen = true;
    private bool _isDarkMode;
    private bool _themeChosenByUser;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _isDarkMode = await _themeProvider.GetSystemDarkModeAsync();
        await _themeProvider.WatchSystemDarkModeAsync(OnSystemPreferenceChanged);

        StateHasChanged();
    }

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        _themeChosenByUser = true;
    }

    private Task OnSystemPreferenceChanged(bool dark)
    {
        if (_themeChosenByUser)
        {
            return Task.CompletedTask;
        }

        _isDarkMode = dark;
        StateHasChanged();

        return Task.CompletedTask;
    }
}
