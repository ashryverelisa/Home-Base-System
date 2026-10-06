using HomeBase.Components.Shared;
using HomeBase.Database.Entities;
using HomeBase.Features.Common;
using HomeBase.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeBase.Features.Catalog.Pages;

public partial class ProductEdit
{
    [Parameter]
    public int? Id { get; set; }

    [SupplyParameterFromQuery(Name = "gtin")]
    public string? ScannedGtin { get; set; }

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    private Product? _product;
    private Product? _existing;
    private bool _lookingUp;
    private IReadOnlyList<Category> _categories = [];
    private IReadOnlyList<StorageLocation> _locations = [];
    private string? _error;
    private bool _saving;

    private bool IsNew => Id is null;

    private string BackHref =>
        ReturnUrl is null ? "/products" : CultureEndpoints.LocalOrHome(ReturnUrl);

    protected override async Task OnParametersSetAsync()
    {
        _categories = await Catalog.GetCategoriesAsync();
        _locations = await Catalog.GetLocationsAsync();

        _product = Id is { } id
            ? await Catalog.FindAsync(id)
            : new Product { Name = string.Empty, IsFood = true };

        _existing = null;

        if (IsNew && _product is not null && Gtin.Normalize(ScannedGtin) is { } gtin)
        {
            _product.Gtin = gtin;

            if (RendererInfo.IsInteractive)
            {
                await LookUpAsync(gtin);
            }
        }
    }

    private async Task ScanAsync()
    {
        if (_product is null || await BarcodeScanDialog.ShowAsync(Dialogs) is not { } gtin)
        {
            return;
        }

        _product.Gtin = gtin;
        await LookUpAsync(gtin);
    }

    private async Task LoadFromOpenFoodFactsAsync()
    {
        if (Gtin.Normalize(_product?.Gtin) is { } gtin)
        {
            await LookUpAsync(gtin);
        }
    }

    private async Task LookUpAsync(string gtin)
    {
        if (_product is null)
        {
            return;
        }

        _existing =
            await Catalog.FindByGtinAsync(gtin) is { } other && other.Id != _product.Id
                ? other
                : null;

        if (_existing is not null)
        {
            return;
        }

        _lookingUp = true;
        StateHasChanged();

        try
        {
            var lookup = await OpenFoodFacts.LookupAsync(gtin);

            switch (lookup)
            {
                case { Status: OffLookupStatus.Found, Product: { HasDetails: true } found }:
                    found.ApplyTo(_product);
                    Snackbar.Add(Localizer["OpenFoodFacts.Applied"], Severity.Success);
                    break;
                case { Status: OffLookupStatus.Unavailable }:
                    Snackbar.Add(Localizer["OpenFoodFacts.Unavailable"], Severity.Warning);
                    break;
                default:
                    Snackbar.Add(Localizer["OpenFoodFacts.NotFound", gtin], Severity.Info);
                    break;
            }
        }
        finally
        {
            _lookingUp = false;
        }
    }

    private async Task SaveAsync()
    {
        if (_product is null)
        {
            return;
        }

        _saving = true;
        _error = null;

        var result = await Catalog.SaveAsync(_product);

        if (!result.Succeeded)
        {
            _error = result.Error;
            _saving = false;
            return;
        }

        _saving = false;
        Navigation.NavigateTo(
            ReturnUrl is null
                ? "/products"
                : Navigation.GetUriWithQueryParameters(
                    BackHref,
                    new Dictionary<string, object?> { ["productId"] = result.ProductId }
                )
        );
    }
}
