using HomeBase.Components.Shared;
using HomeBase.Database.Entities;
using HomeBase.Features.Catalog;
using HomeBase.Features.Common;
using Microsoft.AspNetCore.Components;

namespace HomeBase.Features.Inventory.Pages;

public partial class BookIn
{
    private const string Self = "/inventory/book-in";

    [SupplyParameterFromQuery]
    public int? ProductId { get; set; }

    private IReadOnlyList<ProductRow>? _matches;
    private IReadOnlyList<StorageLocation> _locations = [];
    private Product? _selected;
    private string _term = string.Empty;
    private decimal _packages = 1;
    private decimal _quantityBase;
    private int? _locationId;
    private DateOnly? _bestBefore;
    private bool _rememberShelfLife;
    private string? _error;
    private bool _busy;

    private DateTime? BestBeforeDate
    {
        get => _bestBefore?.ToDateTime(TimeOnly.MinValue);
        set => _bestBefore = value is { } date ? DateOnly.FromDateTime(date) : null;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private int? LearnableShelfLife =>
        ShelfLife.Learnable(_bestBefore, _selected?.DefaultShelfLifeDays, Today);

    private string ShelfLifeHint =>
        _selected?.DefaultShelfLifeDays is { } days
            ? Localizer["BookIn.ShelfLifeHint", days]
            : Localizer["BookIn.NoBestBeforeHint"];

    protected override async Task OnInitializedAsync()
    {
        _locations = await Catalog.GetLocationsAsync();
        _matches = await Catalog.SearchAsync();

        if (ProductId is { } productId)
        {
            await SelectAsync(productId);
        }
    }

    private async Task SearchAsync(string? value)
    {
        _term = value ?? string.Empty;

        if (Gtin.Normalize(_term) is { } gtin && await Catalog.FindByGtinAsync(gtin) is { } hit)
        {
            await SelectAsync(hit.Id);
            return;
        }

        _matches = await Catalog.SearchAsync(_term);
    }

    private async Task ScanAsync()
    {
        if (await BarcodeScanDialog.ShowAsync(Dialogs) is not { } gtin)
        {
            return;
        }

        if (await Catalog.FindByGtinAsync(gtin) is { } product)
        {
            await SelectAsync(product.Id);
            return;
        }

        Navigation.NavigateTo(NewProductHref(gtin));
    }

    private string NewProductHref(string? gtin) =>
        Navigation.GetUriWithQueryParameters(
            "/products/new",
            new Dictionary<string, object?> { ["gtin"] = gtin, ["returnUrl"] = Self }
        );

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
        _bestBefore = _selected.DefaultShelfLifeDays is { } days ? Today.AddDays(days) : null;
        _rememberShelfLife = false;
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

    private async Task ResetAsync()
    {
        _selected = null;
        _term = string.Empty;
        _error = null;
        _matches = await Catalog.SearchAsync();
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

            if (_rememberShelfLife && LearnableShelfLife is { } shelfLifeDays)
            {
                await Catalog.LearnShelfLifeAsync(_selected.Id, shelfLifeDays);
            }

            Navigation.NavigateTo("/inventory");
        }
        finally
        {
            _busy = false;
        }
    }
}
