using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace HomeBase.Features.Assets.Pages;

public sealed partial class AssetEdit
{
    private static readonly AssetStatus[] Statuses = Enum.GetValues<AssetStatus>();
    private static readonly AssetDocumentType[] DocumentTypes = Enum.GetValues<AssetDocumentType>();

    private Asset? _asset;
    private IReadOnlyList<Category> _categories = [];
    private IReadOnlyList<StorageLocation> _locations = [];
    private IReadOnlyList<AssetDocumentRow> _documents = [];
    private AssetDocumentType _documentType = AssetDocumentType.Invoice;
    private string _attributeKey = string.Empty;
    private string _attributeValue = string.Empty;
    private string? _uploadError;
    private string? _error;
    private bool _busy;

    [Parameter]
    public int Id { get; set; }

    private string Title =>
        Id == 0 ? Localizer["Assets.New"] : _asset?.Name ?? Localizer["Assets.Title"].Value;

    private DateTime? PurchasedDate
    {
        get => _asset?.PurchasedAt?.ToDateTime(TimeOnly.MinValue);
        set => SetDate(value, date => _asset!.PurchasedAt = date);
    }

    private DateTime? WarrantyDate
    {
        get => _asset?.WarrantyUntil?.ToDateTime(TimeOnly.MinValue);
        set => SetDate(value, date => _asset!.WarrantyUntil = date);
    }

    private DateTime? ServiceDate
    {
        get => _asset?.NextServiceAt?.ToDateTime(TimeOnly.MinValue);
        set => SetDate(value, date => _asset!.NextServiceAt = date);
    }

    protected override async Task OnParametersSetAsync()
    {
        _categories = await Catalog.GetCategoriesAsync();
        _locations = await Catalog.GetLocationsAsync();

        _asset = Id == 0 ? new Asset { Name = string.Empty } : await Equipment.FindAsync(Id);

        if (Id > 0 && _asset is not null)
        {
            _documents = await Equipment.GetDocumentsAsync(Id);
        }
    }

    private void SetDate(DateTime? value, Action<DateOnly?> assign)
    {
        if (_asset is null)
        {
            return;
        }

        assign(value is { } date ? DateOnly.FromDateTime(date) : null);
    }

    private void AddAttribute()
    {
        if (_asset is null || string.IsNullOrWhiteSpace(_attributeKey))
        {
            return;
        }

        _asset.Attributes[_attributeKey.Trim()] = _attributeValue.Trim();
        _attributeKey = string.Empty;
        _attributeValue = string.Empty;
    }

    private void RemoveAttribute(string key) => _asset?.Attributes.Remove(key);

    private async Task SaveAsync()
    {
        if (_asset is null)
        {
            return;
        }

        _busy = true;
        _error = null;

        try
        {
            var result = await Equipment.SaveAsync(_asset);

            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            Navigation.NavigateTo(Id == 0 ? $"/assets/{result.AssetId}" : "/assets");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CompleteServiceAsync()
    {
        if (_asset is null)
        {
            return;
        }

        _asset.NextServiceAt = await Equipment.CompleteServiceAsync(Id);
    }

    private async Task UploadAsync(IBrowserFile? file)
    {
        if (file is null || Id == 0)
        {
            return;
        }

        _busy = true;
        _uploadError = null;

        try
        {
            if (file.Size > AssetDocumentStore.MaximumFileSize)
            {
                _uploadError = Localizer[
                    "Assets.FileTooLarge",
                    AssetDocumentStore.MaximumFileSize / (1024 * 1024)
                ];

                return;
            }

            await using var stream = file.OpenReadStream(AssetDocumentStore.MaximumFileSize);

            await Equipment.AddDocumentAsync(Id, _documentType, file.Name, stream);

            _documents = await Equipment.GetDocumentsAsync(Id);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task DeleteDocumentAsync(AssetDocumentRow document)
    {
        await Equipment.DeleteDocumentAsync(document.Id);

        _documents = await Equipment.GetDocumentsAsync(Id);
    }
}
