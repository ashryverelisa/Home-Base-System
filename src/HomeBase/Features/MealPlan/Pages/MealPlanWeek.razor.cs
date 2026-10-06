using System.Globalization;
using HomeBase.Components.Shared;
using HomeBase.Database.Enums;
using HomeBase.Features.Common;
using HomeBase.Features.Recipes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace HomeBase.Features.MealPlan.Pages;

public sealed partial class MealPlanWeek
{
    private static readonly MealSlot[] Slots = Enum.GetValues<MealSlot>();

    private readonly List<CookSelection> _cookLines = [];

    private IReadOnlyList<MealPlanRow>? _entries;
    private IReadOnlyList<NeedRow>? _needs;
    private IReadOnlyDictionary<long, decimal> _costs = new Dictionary<long, decimal>();
    private CookPlan? _cookPlan;
    private RecipeRow? _picked;
    private DateOnly _weekStart;
    private DateTime? _newDate;
    private MealSlot _newSlot = MealSlot.Dinner;
    private string _entryText = string.Empty;
    private int _newServings = 2;
    private long? _cookEntryId;
    private string? _cookMessage;
    private string? _listMessage;
    private readonly BusyState _busy = new();

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private DateOnly WeekEnd => _weekStart.AddDays(6);

    private IEnumerable<DateOnly> Days => Enumerable.Range(0, 7).Select(_weekStart.AddDays);

    private string WeekLabel =>
        $"{_weekStart.ToString("d", CultureInfo.CurrentCulture)} – {WeekEnd.ToString("d", CultureInfo.CurrentCulture)}";

    protected override async Task OnInitializedAsync()
    {
        _weekStart = PlanWeek.StartOf(Time.Today());
        _newDate = Time.GetLocalNow().Date;

        await LoadAsync();

        var query = QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query);

        if (
            query.TryGetValue("cook", out var raw)
            && long.TryParse(raw, CultureInfo.InvariantCulture, out var entryId)
            && _entries?.FirstOrDefault(e => e.Id == entryId) is { } entry
        )
        {
            await BeginCookAsync(entry);
        }
    }

    private async Task LoadAsync()
    {
        _entries = await Plan.GetRangeAsync(_weekStart, WeekEnd);
        _needs = await Plan.GetNeedsAsync(_weekStart, WeekEnd);
        _costs = await Plan.GetCostsAsync(_weekStart, WeekEnd);
    }

    private async Task ShiftWeekAsync(int days)
    {
        _weekStart = _weekStart.AddDays(days);
        CancelCook();

        await LoadAsync();
    }

    private async Task GoToTodayAsync()
    {
        _weekStart = PlanWeek.StartOf(Time.Today());
        CancelCook();

        await LoadAsync();
    }

    private async Task<IEnumerable<RecipeRow>> SearchRecipesAsync(
        string? term,
        CancellationToken ct
    ) => await Recipes.SearchAsync(term, ct);

    private void PickRecipe(RecipeRow? row)
    {
        _picked = row;

        if (row is not null)
        {
            _newServings = row.Servings;
        }
    }

    private async Task AddEntryAsync()
    {
        if (_picked is null && string.IsNullOrWhiteSpace(_entryText))
        {
            return;
        }

        await _busy.RunAsync(async () =>
        {
            await Plan.AddAsync(
                _newDate is { } date
                    ? DateOnly.FromDateTime(date)
                    : Time.Today(),
                _newSlot,
                _picked?.Id,
                _picked is null ? _entryText : null,
                _newServings
            );

            _picked = null;
            _entryText = string.Empty;

            await LoadAsync();
        });
    }

    private async Task BeginCookAsync(MealPlanRow entry)
    {
        _cookEntryId = entry.Id;
        _cookMessage = null;
        _cookPlan = await Plan.GetCookPlanAsync(entry.Id);

        _cookLines.Clear();

        if (_cookPlan is null)
        {
            return;
        }

        _cookLines.AddRange(_cookPlan.Lines.Select(CookSelection.For));
    }

    private void CancelCook()
    {
        _cookEntryId = null;
        _cookPlan = null;
        _cookMessage = null;
        _cookLines.Clear();
    }

    private async Task ConfirmCookAsync()
    {
        if (_cookEntryId is not { } entryId)
        {
            return;
        }

        await _busy.RunAsync(async () =>
        {
            var result = await Plan.CookAsync(entryId, CookSelection.ToBook(_cookLines));

            if (!result.Succeeded)
            {
                _cookMessage = Localizer[
                    "Plan.PartiallyBooked",
                    string.Join(", ", result.Missing.Select(line => line.Name))
                ];
            }

            CancelCook();

            await LoadAsync();
        });
    }

    private async Task SkipAsync(MealPlanRow entry)
    {
        await Plan.SetStatusAsync(entry.Id, MealPlanStatus.Skipped);
        await LoadAsync();
    }

    private async Task RemoveAsync(MealPlanRow entry)
    {
        if (_cookEntryId == entry.Id)
        {
            CancelCook();
        }

        await Plan.RemoveAsync(entry.Id);
        await LoadAsync();
    }

    private async Task ApplyToListAsync()
    {
        await _busy.RunAsync(async () =>
        {
            var added = await Plan.ApplyToShoppingListAsync(_weekStart, WeekEnd);

            _listMessage = Localizer["Plan.ListUpdated", added];
        });
    }

    private static string DayLabel(DateOnly day) =>
        day.ToString("dddd, d. MMMM", CultureInfo.CurrentCulture);
}
