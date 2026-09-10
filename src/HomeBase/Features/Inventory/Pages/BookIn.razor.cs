using HomeBase.Database.Entities;
using HomeBase.Features.Catalog;

namespace HomeBase.Features.Inventory.Pages;

public partial class BookIn
{
    private IReadOnlyList<ProductRow>? _matches;
    private IReadOnlyList<StorageLocation> _locations = [];
    private Product? _selected;
    private string _term = string.Empty;
    private decimal _packages = 1;
    private decimal _quantityBase;
    private int? _locationId;
    private DateOnly? _bestBefore;
    private string? _error;
    private bool _busy;

    private DateTime? BestBeforeDate
    {
        get => _bestBefore?.ToDateTime(TimeOnly.MinValue);
        set => _bestBefore = value is { } date ? DateOnly.FromDateTime(date) : null;
    }

    private string ShelfLifeHint =>
        _selected?.DefaultShelfLifeDays is { } days
            ? Localizer["BookIn.ShelfLifeHint", days]
            : Localizer["BookIn.NoBestBeforeHint"];

    protected override async Task OnInitializedAsync()
    {
        _locations = await Catalog.GetLocationsAsync();
        _matches = await Catalog.SearchAsync();
    }

    private async Task SearchAsync(string? value)
    {
        _term = value ?? string.Empty;
        _matches = await Catalog.SearchAsync(_term);
    }

    private async Task SelectAsync(int productId)
    {
        _selected = await Catalog.FindAsync(productId);

        if (_selected is null)
        {
            return;
        }

        _packages = 1;
        _quantityBase = _selected.PackageSize;
        _locationId = _selected.DefaultLocationId;
        _bestBefore = _selected.DefaultShelfLifeDays is { } days
            ? DateOnly.FromDateTime(DateTime.Today).AddDays(days)
            : null;
        _error = null;
    }

    private void OnPackagesChanged(decimal value)
    {
        _packages = value;

        if (_selected is not null)
        {
            _quantityBase = _packages * _selected.PackageSize;
        }
    }

    private void Reset()
    {
        _selected = null;
        _error = null;
    }

    private async Task SaveAsync()
    {
        if (_selected is null)
        {
            return;
        }

        if (_quantityBase <= 0)
        {
            _error = Localizer["Common.AmountRequired"];
            return;
        }

        _busy = true;
        _error = null;

        try
        {
            await Inventory.BookInAsync(
                new BookInRequest(_selected.Id, _quantityBase, _locationId, _bestBefore)
            );

            Navigation.NavigateTo("/inventory");
        }
        finally
        {
            _busy = false;
        }
    }
}
