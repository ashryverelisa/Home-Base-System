using HomeBase.Features.Common;
using HomeBase.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace HomeBase.Components.Shared;

public partial class BarcodeScanDialog
{
    private enum ScanStatus
    {
        Starting,
        Running,
        Insecure,
        Denied,
        Unavailable,
    }

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = null!;

    private ElementReference _video;
    private IJSObjectReference? _module;
    private DotNetObjectReference<BarcodeScanDialog>? _self;
    private ScanStatus _status = ScanStatus.Starting;
    private string? _manual;
    private bool _invalid;

    private string StatusMessage =>
        Localizer[
            _status switch
            {
                ScanStatus.Insecure => "Scan.Insecure",
                ScanStatus.Denied => "Scan.Denied",
                _ => "Scan.Unavailable",
            }
        ];

    public static async Task<string?> ShowAsync(IDialogService dialogs)
    {
        var dialog = await dialogs.ShowAsync<BarcodeScanDialog>(
            AppStrings.Get("Scan.Title"),
            new DialogOptions
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Small,
                CloseButton = true,
            }
        );

        var result = await dialog.Result;

        return result is { Canceled: false, Data: string code } ? code : null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            _module = await Js.InvokeAsync<IJSObjectReference>(
                "import",
                Assets["Components/Shared/BarcodeScanDialog.razor.js"]
            );
            _self = DotNetObjectReference.Create(this);

            var status = await _module.InvokeAsync<string>("start", _video, _self);

            _status = status switch
            {
                "running" => ScanStatus.Running,
                "insecure" => ScanStatus.Insecure,
                "denied" => ScanStatus.Denied,
                _ => ScanStatus.Unavailable,
            };
        }
        catch (JSException)
        {
            _status = ScanStatus.Unavailable;
        }

        StateHasChanged();
    }

    [JSInvokable]
    public bool OnDetected(string code)
    {
        if (Gtin.Normalize(code) is not { } gtin)
        {
            return false;
        }

        Dialog.Close(DialogResult.Ok(gtin));
        return true;
    }

    private void UseManual()
    {
        if (Gtin.Normalize(_manual) is { } gtin)
        {
            Dialog.Close(DialogResult.Ok(gtin));
            return;
        }

        _invalid = true;
    }

    private void OnManualKeyUp(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            UseManual();
        }
        else
        {
            _invalid = false;
        }
    }

    private void Cancel() => Dialog.Cancel();

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("stop");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already gone; the browser released the camera with the page.
            }
        }

        _self?.Dispose();
        GC.SuppressFinalize(this);
    }
}
