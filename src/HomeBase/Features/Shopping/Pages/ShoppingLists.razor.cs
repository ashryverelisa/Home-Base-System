using HomeBase.Components.Shared;
using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Common;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace HomeBase.Features.Shopping.Pages;

public sealed partial class ShoppingLists
{
    private IReadOnlyList<ShoppingListRow> _lists = [];
    private IReadOnlyList<ShoppingItemRow>? _items;
    private IReadOnlyList<ShoppingItemRow>? _settled;
    private ProductRow? _picked;
    private string _term = string.Empty;
    private decimal? _quantity;
    private decimal? _price;
    private string? _link;
    private ShoppingPriority _priority = ShoppingPriority.Normal;
    private int _listId;
    private bool _showSettled;
    private string? _error;
    private readonly BusyState _busy = new();

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
        _price = null;
        _link = null;
        _priority = ShoppingPriority.Normal;
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
        _quantity = row?.PackageSize;
        _error = null;
    }

    private async Task AddAsync()
    {
        _error = null;

        await _busy.RunAsync(async () =>
        {
            var price = TracksPrices ? _price : null;
            var priority = TracksPrices ? _priority : ShoppingPriority.Normal;
            var link = TracksPrices ? _link : null;

            var result = _picked is { } product
                ? await Shopping.AddProductAsync(
                    new AddProductRequest(
                        _listId,
                        product.Id,
                        _quantity > 0 ? _quantity : null,
                        TargetPrice: price,
                        Priority: priority,
                        Link: link
                    )
                )
                : await Shopping.AddFreeTextAsync(
                    new AddFreeTextRequest(
                        _listId,
                        _term,
                        Quantity: TracksPrices && _quantity > 0 ? _quantity : null,
                        TargetPrice: price,
                        Priority: priority,
                        Link: link
                    )
                );

            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            _picked = null;
            _term = string.Empty;
            _quantity = null;
            _price = null;
            _link = null;
            _priority = ShoppingPriority.Normal;

            await LoadItemsAsync();
        });
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

    private async Task SetPriorityAsync(ShoppingItemRow item, ShoppingPriority priority)
    {
        await Shopping.SetPriorityAsync(item.Id, priority);
        await LoadItemsAsync();
    }

    private async Task SetPriceAsync(ShoppingItemRow item, decimal? price)
    {
        await Shopping.SetTargetPriceAsync(item.Id, price);
        await LoadItemsAsync();
    }

    private async Task SetLinkAsync(ShoppingItemRow item, string? link)
    {
        var result = await Shopping.SetLinkAsync(item.Id, link);
        _error = result.Error;

        await LoadItemsAsync();
    }

    private async Task SetQuantityAsync(ShoppingItemRow item, decimal? quantity)
    {
        await Shopping.SetQuantityAsync(item.Id, quantity);
        await LoadItemsAsync();
    }

    private async Task OnAddKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await AddAsync();
        }
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

    private ShoppingListRow? CurrentList => _lists.FirstOrDefault(l => l.Id == _listId);

    private bool TracksPrices => CurrentList?.TracksPrices ?? false;

    private IEnumerable<ShoppingListRow> PricedLists => _lists.Where(l => l.TracksPrices);

    private string PriorityName(ShoppingPriority priority) =>
        Localizer[$"Shopping.Priority.{priority}"];

    private static string PriorityIcon(ShoppingPriority priority) =>
        priority switch
        {
            ShoppingPriority.Urgent => Icons.Material.Filled.PriorityHigh,
            ShoppingPriority.High => Icons.Material.Filled.KeyboardDoubleArrowUp,
            ShoppingPriority.Low => Icons.Material.Filled.KeyboardArrowDown,
            _ => Icons.Material.Filled.DragHandle,
        };

    private static Color PriorityColor(ShoppingPriority priority) =>
        priority switch
        {
            ShoppingPriority.Urgent => Color.Error,
            ShoppingPriority.High => Color.Warning,
            ShoppingPriority.Low => Color.Info,
            _ => Color.Default,
        };

    private static string? UnitText(ShoppingItemRow item) =>
        item.BaseUnit is { } unit ? Units.Abbreviation(unit) : item.Unit;

    private string Describe(ShoppingItemRow item, bool withQuantity = true)
    {
        var parts = new List<string>(3);

        if (withQuantity && item.Quantity is { } quantity)
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

        if (item is { StockBase: { } stock, BaseUnit: { } baseUnit })
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
