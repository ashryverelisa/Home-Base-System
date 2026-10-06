using HomeBase.Database.Entities;

namespace HomeBase.Features.MasterData.Pages;

public partial class LocationsTab
{
    private IReadOnlyList<LocationRow>? _rows;
    private StorageLocation? _editing;

    private bool EditingHasChildren =>
        _editing is { Id: not 0 } && _rows?.Any(r => r.ParentId == _editing.Id) == true;

    private IReadOnlyList<LocationRow> ParentOptions =>
        _editing is null || _rows is null
            ? []
            : TwoLevelTree.ParentOptions(_rows, _editing.Id, EditingHasChildren);

    protected override async Task LoadAsync() => _rows = await MasterData.GetLocationsAsync();

    protected override void CloseEditor() => _editing = null;

    private void New()
    {
        _editing = new StorageLocation { Name = string.Empty };
        Error = null;
    }

    private void Edit(LocationRow row)
    {
        _editing = new StorageLocation
        {
            Id = row.Id,
            Name = row.Name,
            ParentId = row.ParentId,
            Zone = row.Zone,
        };
        Error = null;
    }

    private async Task SaveAsync()
    {
        if (_editing is { } location)
        {
            await RunAsync(() => MasterData.SaveLocationAsync(location));
        }
    }

    private async Task DeleteAsync()
    {
        if (_editing is { } location && await ConfirmDeleteAsync(location.Name))
        {
            await RunAsync(() => MasterData.DeleteLocationAsync(location.Id));
        }
    }
}
