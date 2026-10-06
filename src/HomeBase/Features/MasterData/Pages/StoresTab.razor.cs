using HomeBase.Database.Entities;

namespace HomeBase.Features.MasterData.Pages;

public partial class StoresTab
{
    private IReadOnlyList<StoreRow>? _rows;
    private Store? _editing;

    protected override async Task LoadAsync() => _rows = await MasterData.GetStoresAsync();

    protected override void CloseEditor() => _editing = null;

    private void New()
    {
        _editing = new Store { Name = string.Empty };
        Error = null;
    }

    private void Edit(StoreRow row)
    {
        _editing = new Store
        {
            Id = row.Id,
            Name = row.Name,
            Chain = row.Chain,
            Address = row.Address,
            IsOnline = row.IsOnline,
            TaxId = row.TaxId,
        };
        Error = null;
    }

    private async Task SaveAsync()
    {
        if (_editing is { } store)
        {
            await RunAsync(() => MasterData.SaveStoreAsync(store));
        }
    }

    private async Task DeleteAsync()
    {
        if (_editing is { } store && await ConfirmDeleteAsync(store.Name))
        {
            await RunAsync(() => MasterData.DeleteStoreAsync(store.Id));
        }
    }
}
