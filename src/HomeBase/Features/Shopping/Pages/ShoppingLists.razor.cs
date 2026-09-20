using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Common;

namespace HomeBase.Features.Shopping.Pages;

public sealed partial class ShoppingLists
{
    private IReadOnlyList<ShoppingListRow> _lists = [];
    private IReadOnlyList<ShoppingItemRow>? _items;
    private IReadOnlyList<ShoppingItemRow>? _settled;
    private ProductRow? _picked;
    private string _term = string.Empty;
    private decimal _quantity;
    private int _listId;
    private bool _showSettled;
    private string? _error;
    private bool _busy;

    protected override async Task OnInitializedAsync()
    {
        _lists = await Shopping.GetListsAsync();

        if (_lists.Count == 0)
        {
            return;
        }

        _listId = (_lists.FirstOrDefault(l => l.IsDefault) ?? _lists[0]).Id;

        await LoadItemsAsync();
    }

    private async Task SelectListAsync(int listId)
    {
        _listId = listId;
        _picked = null;
        _term = string.Empty;
        _error = null;

        await LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        if (_listId == 0)
        {
            return;
        }

        _items = await Shopping.GetItemsAsync(_listId);
        _settled = _showSettled ? await Shopping.GetItemsAsync(_listId, settled: true) : null;
        _lists = await Shopping.GetListsAsync();
    }

    private async Task<IEnumerable<ProductRow>> SearchAsync(string? term, CancellationToken ct) =>
        await Catalog.SearchAsync(term, ct);

    private void Pick(ProductRow? row)
    {
        _picked = row;
        _quantity = row?.PackageSize ?? 0;
        _error = null;
    }

    private async Task AddAsync()
    {
        _busy = true;
        _error = null;

        try
        {
            var result = _picked is { } product
                ? await Shopping.AddProductAsync(
                    new AddProductRequest(_listId, product.Id, _quantity > 0 ? _quantity : null)
                )
                : await Shopping.AddFreeTextAsync(new AddFreeTextRequest(_listId, _term));

            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            _picked = null;
            _term = string.Empty;
            _quantity = 0;

            await LoadItemsAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task BuyAsync(ShoppingItemRow item)
    {
        await Shopping.SetStatusAsync(item.Id, ShoppingListItemStatus.Bought);
        await LoadItemsAsync();
    }

    private async Task ReopenAsync(ShoppingItemRow item)
    {
        await Shopping.SetStatusAsync(item.Id, ShoppingListItemStatus.Open);
        await LoadItemsAsync();
    }

    private async Task ToggleImportantAsync(ShoppingItemRow item)
    {
        await Shopping.ToggleImportantAsync(item.Id);
        await LoadItemsAsync();
    }

    private async Task RemoveAsync(ShoppingItemRow item)
    {
        await Shopping.RemoveAsync(item.Id);
        await LoadItemsAsync();
    }

    private async Task ToggleSettledAsync()
    {
        _showSettled = !_showSettled;

        await LoadItemsAsync();
    }

    private async Task ClearSettledAsync()
    {
        await Shopping.ClearSettledAsync(_listId);
        await LoadItemsAsync();
    }

    private string Describe(ShoppingItemRow item)
    {
        var parts = new List<string>(3);

        if (item.Quantity is { } quantity)
        {
            parts.Add(
                item.BaseUnit is { } unit
                    ? Units.Format(quantity, unit)
                    : $"{quantity:0.###} {item.Unit}".TrimEnd()
            );
        }

        if (!string.IsNullOrWhiteSpace(item.Brand))
        {
            parts.Add(item.Brand);
        }

        if (item.StockBase is { } stock && item.BaseUnit is { } baseUnit)
        {
            parts.Add(Localizer["BookIn.CurrentStock", Units.Format(stock, baseUnit)]);
        }

        if (!string.IsNullOrWhiteSpace(item.Note))
        {
            parts.Add(item.Note);
        }

        return string.Join(" · ", parts);
    }
}
