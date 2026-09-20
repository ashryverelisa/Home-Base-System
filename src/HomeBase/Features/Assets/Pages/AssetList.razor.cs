using System.Globalization;
using MudBlazor;

namespace HomeBase.Features.Assets.Pages;

public sealed partial class AssetList
{
    private IReadOnlyList<AssetRow>? _rows;
    private string _term = string.Empty;
    private bool _warrantyOnly;
    private bool _serviceOnly;
    private bool _includeRetired;

    private bool WarrantyFilter
    {
        get => _warrantyOnly;
        set
        {
            _warrantyOnly = value;
            _ = LoadAsync();
        }
    }

    private bool ServiceFilter
    {
        get => _serviceOnly;
        set
        {
            _serviceOnly = value;
            _ = LoadAsync();
        }
    }

    private bool RetiredFilter
    {
        get => _includeRetired;
        set
        {
            _includeRetired = value;
            _ = LoadAsync();
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task SearchAsync(string? value)
    {
        _term = value ?? string.Empty;

        return LoadAsync();
    }

    private async Task LoadAsync()
    {
        _rows = await Equipment.SearchAsync(
            new AssetFilter(_term, _includeRetired, _warrantyOnly, _serviceOnly)
        );

        StateHasChanged();
    }

    private string WarrantyText(AssetRow row) =>
        row.WarrantyDaysLeft switch
        {
            null => Localizer["Assets.NoWarranty"],
            < 0 => Localizer["Assets.WarrantyExpired", row.WarrantyUntil!.Value.ToString("d", CultureInfo.CurrentCulture)],
            var days => Localizer["Assets.WarrantyLeft", days, row.WarrantyUntil!.Value.ToString("d", CultureInfo.CurrentCulture)],
        };

    private static Color WarrantyColor(AssetRow row) =>
        row switch
        {
            { WarrantyExpired: true } => Color.Default,
            { WarrantyEndingSoon: true } => Color.Warning,
            _ => Color.Default,
        };
}
