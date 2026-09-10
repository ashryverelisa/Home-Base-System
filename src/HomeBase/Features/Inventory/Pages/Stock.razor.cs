using HomeBase.Database.Enums;
using HomeBase.Features.Common;
using MudBlazor;

namespace HomeBase.Features.Inventory.Pages;

public partial class Stock
{
    private IReadOnlyList<StockLotView>? _lots;
    private int? _withinDays;
    private StockLotView? _active;
    private StockMovementType _activeType = StockMovementType.Consume;
    private decimal _amount;
    private string? _message;
    private bool _busy;

    private string MovementLabel =>
        Localizer[
            _activeType == StockMovementType.Waste ? "Stock.AmountWasted" : "Stock.AmountConsumed"
        ];

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _lots = await Inventory.GetStockAsync(withinDays: _withinDays);
    }

    private async Task SetFilterAsync(int? days)
    {
        _withinDays = days;
        Cancel();
        await LoadAsync();
    }

    private void Begin(StockLotView lot, StockMovementType type)
    {
        _active = lot;
        _activeType = type;
        _amount = 0;
        _message = null;
    }

    private void Cancel()
    {
        _active = null;
        _message = null;
    }

    private Task ApplyAllAsync() => BookAsync(_active?.QuantityBase ?? 0);

    private Task ApplyAsync() => BookAsync(_amount);

    private async Task BookAsync(decimal quantity)
    {
        if (_active is null || quantity <= 0)
        {
            _message = Localizer["Common.AmountRequired"];
            return;
        }

        _busy = true;
        _message = null;

        try
        {
            var result = await Inventory.TakeFromLotAsync(_active.LotId, quantity, _activeType);

            if (!result.IsComplete)
            {
                _message = Localizer[
                    "Stock.PartiallyBooked",
                    Units.Format(result.Applied, _active.BaseUnit),
                    Units.Format(result.Shortfall, _active.BaseUnit)
                ];
            }

            _active = null;
            await LoadAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OpenAsync(StockLotView lot)
    {
        await Inventory.OpenLotAsync(lot.LotId);
        await LoadAsync();
    }

    private string BestBeforeText(StockLotView lot) =>
        lot.DaysLeft switch
        {
            null => Localizer["Stock.NoBestBefore"],
            < 0 => Localizer["Stock.ExpiredSince", -lot.DaysLeft.Value],
            0 => Localizer["Stock.ExpiresToday"],
            1 => Localizer["Stock.ExpiresTomorrow"],
            var days => Localizer["Stock.DaysLeft", days],
        };

    private static Color BestBeforeColor(StockLotView lot) =>
        lot.DaysLeft switch
        {
            null => Color.Default,
            < 0 => Color.Error,
            <= 3 => Color.Warning,
            _ => Color.Default,
        };
}
