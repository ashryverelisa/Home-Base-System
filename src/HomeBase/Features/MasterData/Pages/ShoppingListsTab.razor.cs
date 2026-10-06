using HomeBase.Database.Entities;

namespace HomeBase.Features.MasterData.Pages;

public partial class ShoppingListsTab
{
    private IReadOnlyList<ShoppingListAdminRow>? _rows;
    private ShoppingList? _editing;

    protected override async Task LoadAsync() =>
        _rows = await MasterData.GetShoppingListsAsync();

    protected override void CloseEditor() => _editing = null;

    private void New()
    {
        _editing = new ShoppingList { Name = string.Empty };
        Error = null;
    }

    private void Edit(ShoppingListAdminRow row)
    {
        _editing = new ShoppingList
        {
            Id = row.Id,
            Name = row.Name,
            Kind = row.Kind,
        };
        Error = null;
    }

    private async Task SaveAsync()
    {
        if (_editing is { } list)
        {
            await RunAsync(() => MasterData.SaveShoppingListAsync(list));
        }
    }

    private Task MakeDefaultAsync(ShoppingListAdminRow row) =>
        RunAsync(() => MasterData.SetDefaultShoppingListAsync(row.Id), closeEditor: false);

    private Task ToggleArchivedAsync(ShoppingListAdminRow row) =>
        RunAsync(
            () => MasterData.SetShoppingListArchivedAsync(row.Id, !row.Archived),
            closeEditor: false
        );

    private async Task MoveAsync(ShoppingListAdminRow row, int offset)
    {
        Error = null;
        await MasterData.MoveShoppingListAsync(row.Id, offset);
        await LoadAsync();
    }
}
