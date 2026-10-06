using HomeBase.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace HomeBase.Features.MasterData.Pages;

public abstract class MasterDataTabBase : ComponentBase
{
    [Inject]
    protected IMasterDataService MasterData { get; set; } = null!;

    [Inject]
    protected IDialogService Dialogs { get; set; } = null!;

    [Inject]
    protected IStringLocalizer<AppStrings> Localizer { get; set; } = null!;

    protected string? Error { get; set; }

    protected bool Busy { get; private set; }

    protected abstract Task LoadAsync();

    protected abstract void CloseEditor();

    protected override Task OnInitializedAsync() => LoadAsync();

    protected void Close()
    {
        CloseEditor();
        Error = null;
    }

    protected async Task RunAsync(Func<Task<MasterDataResult>> action, bool closeEditor = true)
    {
        Busy = true;

        try
        {
            var result = await action();

            if (!result.Succeeded)
            {
                Error = result.Error;
                return;
            }

            if (closeEditor)
            {
                Close();
            }

            await LoadAsync();
        }
        finally
        {
            Busy = false;
        }
    }

    protected async Task<bool> ConfirmDeleteAsync(string name) =>
        await Dialogs.ShowMessageBoxAsync(
            Localizer["MasterData.DeleteTitle"],
            Localizer["MasterData.DeleteConfirm", name],
            yesText: Localizer["MasterData.Delete"],
            cancelText: Localizer["Common.Cancel"]
        ) == true;
}
