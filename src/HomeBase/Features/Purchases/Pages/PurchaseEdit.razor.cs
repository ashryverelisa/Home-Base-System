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
    private bool _busy;

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

    private List<int> ItemLineIndexes =>
        [
            .. Enumerable
                .Range(0, _draft.Lines.Count)
                .Where(index => _draft.Lines[index].IsItem),
        ];

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
        _line.LineType = type;
        _lineError = null;

        if (type != PurchaseLineType.Item)
        {
            _picked = null;
            _line.ProductId = null;
            _line.ProductName = null;
            _line.BaseUnit = null;
            _line.QuantityBase = null;
            _line.IsPromo = false;
            _line.BestBefore = null;
        }

        if (type != PurchaseLineType.Discount)
        {
            _line.ParentIndex = null;
        }
    }

    private void PickProduct(ProductRow? row)
    {
        _picked = row;
        _lineError = null;

        _line.ProductId = row?.Id;
        _line.ProductName = row?.Name;
        _line.BaseUnit = row?.BaseUnit;
        _line.Quantity = 1m;
        _line.QuantityBase = row?.PackageSize;
    }

    private void OnQuantityChanged(decimal value)
    {
        _line.Quantity = value;

        if (_picked is { } product)
        {
            _line.QuantityBase = value * product.PackageSize;
        }
    }

    private void AddLine()
    {
        if (_line.IsItem && _line.ProductId is null)
        {
            _lineError = Localizer["Purchases.ProductRequired"];
            return;
        }

        if (!_line.IsItem && string.IsNullOrWhiteSpace(_line.RawText))
        {
            _lineError = Localizer["Purchases.TextRequired"];
            return;
        }

        if (_line.LineTotal == 0)
        {
            _lineError = Localizer["Purchases.AmountRequired"];
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
        var index = _draft.Lines.IndexOf(line);

        if (index < 0)
        {
            return;
        }

        _draft.Lines.RemoveAt(index);

        foreach (var other in _draft.Lines)
        {
            other.ParentIndex = other.ParentIndex switch
            {
                { } parent when parent == index => null,
                { } parent when parent > index => parent - 1,
                var parent => parent,
            };
        }

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
        _busy = true;
        _error = null;

        try
        {
            var result = await Purchases.SaveAsync(_draft);

            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            Navigation.NavigateTo($"/purchases/{result.PurchaseId}");
        }
        finally
        {
            _busy = false;
        }
    }
}
