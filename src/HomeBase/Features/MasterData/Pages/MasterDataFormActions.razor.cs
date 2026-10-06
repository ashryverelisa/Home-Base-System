using Microsoft.AspNetCore.Components;

namespace HomeBase.Features.MasterData.Pages;

public partial class MasterDataFormActions
{
    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public bool CanDelete { get; set; }

    [Parameter]
    public EventCallback OnSave { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }
}
