using HomeBase.Components.Shared;
using HomeBase.Database.Entities;
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
    private readonly BusyState _busy = new();
    private IReadOnlyList<StorageLocation> _locations = [];
    private StockLotView? _moving;
    private int? _moveLocationId;
    private DateOnly? _moveBestBefore;

    private StorageZone? MoveTargetZone =>
        _locations.FirstOrDefault(l => l.Id == _moveLocationId)?.Zone;

    private bool MoveChangesShelfLife =>
        _moving is not null && Zones.ChangesShelfLife(_moving.LocationZone, MoveTargetZone);

    private DateTime? MoveBestBeforeDate
    {
        get => _moveBestBefore?.ToDateTime(TimeOnly.MinValue);
        set => _moveBestBefore = value is { } date ? DateOnly.FromDateTime(date) : null;
    }

    private string MovementLabel =>
        Localizer[
            _activeType == StockMovementType.Waste ? "Stock.AmountWasted" : "Stock.AmountConsumed"
        ];

    protected override async Task OnInitializedAsync()
    {
        _locations = await Catalog.GetLocationsAsync();
        await LoadAsync();
    }

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

    private void BeginMove(StockLotView lot)
    {
        Cancel();
        _moving = lot;
        _moveLocationId = lot.LocationId;
        _moveBestBefore = lot.BestBefore;
    }

    private void CancelMove() => _moving = null;

    private async Task MoveAsync()
    {
        if (_moving is null)
        {
            return;
        }

        await _busy.RunAsync(async () =>
        {
            await Inventory.MoveLotAsync(
                new LotMove(_moving.LotId, _moveLocationId, MoveChangesShelfLife, _moveBestBefore)
            );

            _moving = null;
            await LoadAsync();
        });
    }

    private void Begin(StockLotView lot, StockMovementType type)
    {
        _moving = null;
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

        _message = null;

        await _busy.RunAsync(async () =>
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
        });
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
        lot.IsExpired ? Color.Error
        : lot.ExpiresSoon ? Color.Warning
        : Color.Default;
}
