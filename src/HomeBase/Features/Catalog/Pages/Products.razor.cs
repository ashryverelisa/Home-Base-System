namespace HomeBase.Features.Catalog.Pages;

public sealed partial class Products
{
    private IReadOnlyList<ProductRow>? _rows;
    private string _term = string.Empty;
    private CancellationTokenSource? _pending;

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task SearchAsync(string? value)
    {
        _term = value ?? string.Empty;

        return LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_pending is not null)
        {
            await _pending.CancelAsync();
            _pending.Dispose();
        }

        _pending = new CancellationTokenSource();
        var token = _pending.Token;

        try
        {
            _rows = await Catalog.SearchAsync(_term, token);
        }
        catch (OperationCanceledException)
        {
            // superseded
        }
    }

    public void Dispose() => _pending?.Dispose();
}
