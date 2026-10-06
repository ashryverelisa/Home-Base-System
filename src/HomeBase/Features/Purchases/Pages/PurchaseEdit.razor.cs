using HomeBase.Components.Shared;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Common;

namespace HomeBase.Features.Purchases.Pages;

public sealed partial class PurchaseEdit
{
    private static readonly PurchaseLineType[] LineTypes =
    [
        PurchaseLineType.Item,
        PurchaseLineType.Deposit,
        PurchaseLineType.DepositReturn,
        PurchaseLineType.Discount,
        PurchaseLineType.Fee,
    ];

    private readonly PurchaseDraft _draft = new();
    private PurchaseDraftLine _line = new();
    private IReadOnlyList<Store> _stores = [];
    private ProductRow? _picked;
    private string? _lineError;
    private string? _error;
    private readonly BusyState _busy = new();

    private DateTime? PurchasedDate
    {
        get => _draft.PurchasedAt.LocalDateTime;
        set =>
            _draft.PurchasedAt = value is { } date
                ? new DateTimeOffset(date, DateTimeOffset.Now.Offset)
                : DateTimeOffset.Now;
    }

    private DateTime? BestBeforeDate
    {
        get => _line.BestBefore?.ToDateTime(TimeOnly.MinValue);
        set => _line.BestBefore = value is { } date ? DateOnly.FromDateTime(date) : null;
    }

    private decimal QuantityBase
    {
        get => _line.QuantityBase ?? 0m;
        set => _line.QuantityBase = value;
    }

    protected override async Task OnInitializedAsync() => _stores = await Purchases.GetStoresAsync();

    private Task<IEnumerable<string>> SearchStoresAsync(string? term, CancellationToken ct) =>
        Task.FromResult<IEnumerable<string>>(
            [
                .. _stores
                    .Select(s => s.Name)
                    .Where(name =>
                        string.IsNullOrWhiteSpace(term)
                        || name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                    ),
            ]
        );

    private async Task<IEnumerable<ProductRow>> SearchProductsAsync(
        string? term,
        CancellationToken ct
    ) => await Catalog.SearchAsync(term, ct);

    private void SetLineType(PurchaseLineType type)
    {
        _line.ChangeType(type);
        _lineError = null;

        if (type != PurchaseLineType.Item)
        {
            _picked = null;
        }
    }

    private void PickProduct(ProductRow? row)
    {
        _picked = row;
        _lineError = null;

        _line.SelectProduct(row);
    }

    private void OnQuantityChanged(decimal value) =>
        _line.ChangeQuantity(value, _picked?.PackageSize);

    private void AddLine()
    {
        if (_line.Validate() is { } error)
        {
            _lineError = Localizer[error];
            return;
        }

        _draft.Lines.Add(_line);

        _line = new PurchaseDraftLine { LineType = _line.LineType };
        _picked = null;
        _lineError = null;
        _error = null;
    }

    private void RemoveLine(PurchaseDraftLine line)
    {
        _draft.RemoveLine(line);
        _error = null;
    }

    private string DescribeLine(PurchaseDraftLine line)
    {
        var parts = new List<string>(3);

        if (line.IsItem)
        {
            parts.Add(Localizer["Purchases.QuantityShort", line.Quantity]);

            if (line.QuantityBase is { } quantityBase && line.BaseUnit is { } unit)
            {
                parts.Add(Units.Format(quantityBase, unit));

                if (quantityBase > 0)
                {
                    parts.Add(Units.PricePerUnit(line.SignedTotal / quantityBase, unit));
                }
            }
        }

        if (line.ParentIndex is { } parentIndex && parentIndex < _draft.Lines.Count)
        {
            parts.Add(Localizer["Purchases.BelongsToShort", _draft.Lines[parentIndex].Label]);
        }

        return string.Join(" · ", parts);
    }

    private async Task SaveAsync()
    {
        _error = null;

        await _busy.RunAsync(async () =>
        {
            var result = await Purchases.SaveAsync(_draft);

            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            Navigation.NavigateTo($"/purchases/{result.Id}");
        });
    }
}
