using HomeBase.Database.Entities;

namespace HomeBase.Features.MasterData.Pages;

public partial class CategoriesTab
{
    private IReadOnlyList<CategoryRow>? _rows;
    private Category? _editing;

    private bool EditingHasChildren =>
        _editing is { Id: not 0 } && _rows?.Any(r => r.ParentId == _editing.Id) == true;

    private IReadOnlyList<CategoryRow> ParentOptions =>
        _editing is null || _rows is null
            ? []
            : TwoLevelTree.ParentOptions(_rows, _editing.Id, EditingHasChildren);

    protected override async Task LoadAsync() => _rows = await MasterData.GetCategoriesAsync();

    protected override void CloseEditor() => _editing = null;

    private void New()
    {
        _editing = new Category { Name = string.Empty };
        Error = null;
    }

    private void Edit(CategoryRow row)
    {
        _editing = new Category
        {
            Id = row.Id,
            Name = row.Name,
            ParentId = row.ParentId,
            Kind = row.Kind,
        };
        Error = null;
    }

    private async Task SaveAsync()
    {
        if (_editing is { } category)
        {
            await RunAsync(() => MasterData.SaveCategoryAsync(category));
        }
    }

    private async Task DeleteAsync()
    {
        if (_editing is { } category && await ConfirmDeleteAsync(category.Name))
        {
            await RunAsync(() => MasterData.DeleteCategoryAsync(category.Id));
        }
    }
}
