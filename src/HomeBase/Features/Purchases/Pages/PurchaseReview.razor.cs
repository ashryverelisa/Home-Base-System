using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using Microsoft.AspNetCore.Components;

namespace HomeBase.Features.Purchases.Pages;

public sealed partial class PurchaseReview
{
    private readonly Dictionary<long, ProductRow?> _picks = [];

    private PurchaseRow? _purchase;
    private IReadOnlyList<ReviewLineRow> _lines = [];
    private decimal _lineSum;
    private int _openCount;
    private bool _confirmed;
    private bool _busy;

    [Parameter]
    public long Id { get; set; }

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _purchase = await Purchases.FindAsync(Id);

        if (_purchase is null)
        {
            return;
        }

        _confirmed = _purchase.Status == PurchaseStatus.Confirmed;
        _lines = await Review.GetLinesAsync(Id);
        _lineSum = await Review.GetLineSumAsync(Id);
        _openCount = _lines.Count(l => l.NeedsAssignment);
    }

    private ProductRow? Pick(ReviewLineRow line) => _picks.GetValueOrDefault(line.Id);

    private async Task<IEnumerable<ProductRow>> SearchProductsAsync(
        string? term,
        CancellationToken ct
    ) => await Catalog.SearchAsync(term, ct);

    private Task AssignAsync(ReviewLineRow line) =>
        Pick(line) is { } product ? ApplyAssignmentAsync(line, product.Id) : Task.CompletedTask;

    private Task AssignSuggestionAsync(ReviewLineRow line, int productId) =>
        ApplyAssignmentAsync(line, productId);

    private async Task ApplyAssignmentAsync(ReviewLineRow line, int productId)
    {
        _busy = true;

        try
        {
            await Review.AssignAsync(line.Id, productId);

            _picks.Remove(line.Id);

            await LoadAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task SetPromoAsync(ReviewLineRow line, bool isPromo)
    {
        await Review.SetPromoAsync(line.Id, isPromo);
        await LoadAsync();
    }

    private async Task ConfirmAsync()
    {
        _busy = true;

        try
        {
            await Purchases.ConfirmAsync(Id);

            Navigation.NavigateTo($"/purchases/{Id}");
        }
        finally
        {
            _busy = false;
        }
    }

    private string MatchLabel(ReviewLineRow line) =>
        line switch
        {
            { LineType: not PurchaseLineType.Item } => Localizer["Review.NoProductNeeded"],
            { ProductId: not null } => line.MatchConfidence is { } confidence
                ? Localizer["Review.MatchedBy", Localizer[$"MatchStatus.{line.MatchStatus}"], confidence]
                : Localizer[$"MatchStatus.{line.MatchStatus}"].Value,
            { SuggestedProductName: { } suggestion } => Localizer["Review.Suggestion", suggestion],
            _ => Localizer["Review.Unmatched"],
        };
}
