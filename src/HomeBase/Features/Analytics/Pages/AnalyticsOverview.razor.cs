using System.Globalization;
using HomeBase.Database.Enums;
using HomeBase.Features.Common;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HomeBase.Features.Analytics.Pages;

public sealed partial class AnalyticsOverview
{
    private IReadOnlyList<MonthlySpendRow>? _months;
    private IReadOnlyList<StoreSpendRow>? _stores;
    private IReadOnlyList<ProductPriceRow>? _products;
    private IReadOnlyList<StorePriceRow>? _comparison;
    private IReadOnlyList<WasteRow>? _waste;
    private IReadOnlyList<WasteProductRow>? _wastedProducts;
    private IReadOnlyList<BasketIndexRow>? _index;
    private IReadOnlyList<PromoSavingRow>? _savings;
    private IReadOnlyList<ReachRow>? _reach;
    private decimal _deposit;
    private int _productId;

    private readonly List<ChartSeries<double>> _spendSeries = [];
    private readonly List<ChartSeries<double>> _trendSeries = [];
    private readonly List<ChartSeries<double>> _indexSeries = [];

    private string[] _spendLabels = [];
    private string[] _trendLabels = [];
    private string[] _indexLabels = [];

    private ChartOptions _spendOptions = new();
    private ChartOptions _priceOptions = new();
    private ChartOptions _indexOptions = new();

    [CascadingParameter(Name = "IsDarkMode")]
    public bool IsDarkMode { get; set; }

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private decimal CurrentMonthTotal =>
        _months?.FirstOrDefault(m =>
            m.Month.Month == DateTime.UtcNow.Month && m.Month.Year == DateTime.UtcNow.Year
        )?.Total ?? 0m;

    private decimal WasteTotal => _waste?.Sum(w => w.Cost) ?? 0m;

    private ProductPriceRow? SelectedProduct =>
        _products?.FirstOrDefault(p => p.ProductId == _productId);

    private string SelectedProductName => SelectedProduct?.Name ?? string.Empty;

    private BaseUnit? SelectedUnit => SelectedProduct?.BaseUnit;

    private string SelectedUnitLabel =>
        SelectedUnit is { } unit ? Units.DisplayAbbreviation(unit) : string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _months = await Analytics.GetMonthlySpendAsync();
        _stores = await Analytics.GetStoreSpendAsync();
        _products = await Analytics.GetProductPricesAsync();
        _deposit = await Analytics.GetDepositBalanceAsync();
        _waste = await Analytics.GetWasteAsync();
        _wastedProducts = await Analytics.GetWastedProductsAsync();
        _index = await Analytics.GetBasketIndexAsync();
        _savings = await Analytics.GetPromoSavingsAsync();
        _reach = await Analytics.GetReachAsync();

        BuildSpendChart();
        BuildIndexChart();

        if (_products is { Count: > 0 })
        {
            await SelectProductAsync(_products[0].ProductId);
        }
    }

    private async Task SelectProductAsync(int productId)
    {
        _productId = productId;
        _comparison = await Analytics.GetStoreComparisonAsync(productId);

        var trend = await Analytics.GetPriceTrendAsync(productId);

        _trendSeries.Clear();
        _trendLabels = [.. trend.Select(p => MonthLabel(p.Month))];

        if (trend.Count > 0)
        {
            _trendSeries.Add(
                new ChartSeries<double>
                {
                    Name = SelectedProductName,
                    Data = new ChartData<double>(
                        [.. trend.Select(p => (double)PerDisplayUnit(p.Price))]
                    ),
                }
            );
        }

        _priceOptions = Options(slot: 0);
    }

    private void BuildSpendChart()
    {
        if (_months is not { Count: > 0 })
        {
            return;
        }

        var ordered = _months.OrderBy(m => m.Month).ToList();

        _spendLabels = [.. ordered.Select(m => MonthLabel(m.Month))];
        _spendSeries.Clear();
        _spendSeries.Add(
            new ChartSeries<double>
            {
                Name = Localizer["Analytics.PerMonth"],
                Data = new ChartData<double>([.. ordered.Select(m => (double)m.Total)]),
            }
        );

        _spendOptions = Options(slot: 0);
    }

    private void BuildIndexChart()
    {
        if (_index is not { Count: > 0 })
        {
            return;
        }

        _indexLabels = [.. _index.Select(p => MonthLabel(p.Month))];
        _indexSeries.Clear();
        _indexSeries.Add(
            new ChartSeries<double>
            {
                Name = Localizer["Analytics.BasketIndex"],
                Data = new ChartData<double>([.. _index.Select(p => (double)p.Index)]),
            }
        );

        _indexOptions = Options(slot: 0);
    }

    private ChartOptions Options(int slot) =>
        new() { ChartPalette = ChartPalette.Slot(IsDarkMode, slot) };

    private decimal PerDisplayUnit(decimal pricePerBaseUnit) =>
        SelectedUnit switch
        {
            BaseUnit.Gram or BaseUnit.Milliliter => pricePerBaseUnit * 1000m,
            _ => pricePerBaseUnit,
        };

    private static string MonthLabel(DateTimeOffset month) =>
        month.ToString("MMM yy", Culture);

    private string StoreLabel(string? name) =>
        string.IsNullOrWhiteSpace(name) ? Localizer["Purchases.NoStore"] : name;
}
