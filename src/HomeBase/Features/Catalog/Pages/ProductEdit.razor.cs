using HomeBase.Database.Entities;
using Microsoft.AspNetCore.Components;

namespace HomeBase.Features.Catalog.Pages;

public partial class ProductEdit
{
    [Parameter]
    public int? Id { get; set; }

    private Product? _product;
    private IReadOnlyList<Category> _categories = [];
    private IReadOnlyList<StorageLocation> _locations = [];
    private string? _error;
    private bool _saving;

    private bool IsNew => Id is null;

    protected override async Task OnParametersSetAsync()
    {
        _categories = await Catalog.GetCategoriesAsync();
        _locations = await Catalog.GetLocationsAsync();

        _product = Id is { } id
            ? await Catalog.FindAsync(id)
            : new Product { Name = string.Empty, IsFood = true };
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
        Navigation.NavigateTo("/products");
    }
}
