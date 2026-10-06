using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;

namespace HomeBase.Features.Catalog;

public enum OffLookupStatus
{
    Found,
    NotFound,
    Unavailable,
}

public sealed record OffProduct(
    string Code,
    string? Name,
    string? Brand,
    BaseUnit BaseUnit,
    decimal PackageSize,
    string? ImageUrl
)
{
    public bool HasDetails => Name is not null || HasPackage || ImageUrl is not null;

    private bool HasPackage => BaseUnit is not BaseUnit.Piece;

    // Fills gaps only: whatever is already maintained on the product wins.
    public void ApplyTo(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name) && Name is not null)
        {
            product.Name = Name;
        }

        if (string.IsNullOrWhiteSpace(product.Brand))
        {
            product.Brand = Brand;
        }

        if (HasPackage && product is { BaseUnit: BaseUnit.Piece, PackageSize: 1m })
        {
            product.BaseUnit = BaseUnit;
            product.PackageSize = PackageSize;
        }

        product.ImageUrl ??= ImageUrl;
        product.OffId = Code;
    }
}

public sealed record OffLookup(OffLookupStatus Status, OffProduct? Product = null);

public interface IOpenFoodFacts
{
    Task<OffLookup> LookupAsync(string gtin, CancellationToken ct = default);
}

public sealed partial class OpenFoodFactsClient(HttpClient http, ILogger<OpenFoodFactsClient> logger)
    : IOpenFoodFacts
{
    private const string Fields =
        "code,product_name,product_name_de,brands,quantity,product_quantity,product_quantity_unit,image_front_url";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Open Food Facts lookup for {Gtin} failed")]
    private static partial void LogLookupFailed(ILogger logger, string gtin, Exception exception);

    public async Task<OffLookup> LookupAsync(string gtin, CancellationToken ct = default)
    {
        try
        {
            using var message = await http.GetAsync(
                $"api/v2/product/{Uri.EscapeDataString(gtin)}.json?fields={Fields}",
                ct
            );

            if (message.StatusCode == HttpStatusCode.NotFound)
            {
                return new OffLookup(OffLookupStatus.NotFound);
            }

            message.EnsureSuccessStatusCode();

            var response = await message.Content.ReadFromJsonAsync<OffResponse>(ct);

            return response is { Status: 1, Product: { } product }
                ? new OffLookup(OffLookupStatus.Found, OpenFoodFactsMapping.ToProduct(gtin, product))
                : new OffLookup(OffLookupStatus.NotFound);
        }
        catch (Exception ex)
            when (ex is HttpRequestException or TaskCanceledException or JsonException
                && !ct.IsCancellationRequested)
        {
            LogLookupFailed(logger, gtin, ex);
            return new OffLookup(OffLookupStatus.Unavailable);
        }
    }
}

internal sealed record OffResponse(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("product")] OffProductDto? Product
);

internal sealed record OffProductDto(
    [property: JsonPropertyName("product_name")] string? ProductName,
    [property: JsonPropertyName("product_name_de")] string? ProductNameDe,
    [property: JsonPropertyName("brands")] string? Brands,
    [property: JsonPropertyName("quantity")] string? Quantity,
    [property: JsonPropertyName("product_quantity")] JsonElement? ProductQuantity,
    [property: JsonPropertyName("product_quantity_unit")] string? ProductQuantityUnit,
    [property: JsonPropertyName("image_front_url")] string? ImageFrontUrl
);

internal static partial class OpenFoodFactsMapping
{
    public static OffProduct ToProduct(string gtin, OffProductDto dto)
    {
        var (unit, size) =
            PackageFrom(Number(dto.ProductQuantity), dto.ProductQuantityUnit)
            ?? PackageFrom(dto.Quantity)
            ?? (BaseUnit.Piece, 1m);

        return new OffProduct(
            gtin,
            Blank(dto.ProductNameDe) ?? Blank(dto.ProductName),
            Blank(dto.Brands?.Split(',')[0]),
            unit,
            size,
            Blank(dto.ImageFrontUrl)
        );
    }

    public static (BaseUnit Unit, decimal Size)? PackageFrom(decimal? amount, string? unit)
    {
        if (amount is not > 0 || string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        return unit.Trim().ToLowerInvariant() switch
        {
            "g" or "gr" or "gramm" => (BaseUnit.Gram, amount.Value),
            "kg" => (BaseUnit.Gram, amount.Value * 1000),
            "ml" => (BaseUnit.Milliliter, amount.Value),
            "cl" => (BaseUnit.Milliliter, amount.Value * 10),
            "dl" => (BaseUnit.Milliliter, amount.Value * 100),
            "l" or "lt" or "liter" or "litre" => (BaseUnit.Milliliter, amount.Value * 1000),
            _ => null,
        };
    }

    public static (BaseUnit Unit, decimal Size)? PackageFrom(string? quantity)
    {
        if (string.IsNullOrWhiteSpace(quantity))
        {
            return null;
        }

        var match = QuantityPattern().Match(quantity);

        return match.Success
            ? PackageFrom(Parse(match.Groups["amount"].Value), match.Groups["unit"].Value)
            : null;
    }

    private static decimal? Number(JsonElement? element) =>
        element switch
        {
            { ValueKind: JsonValueKind.Number } e when e.TryGetDecimal(out var value) => value,
            { ValueKind: JsonValueKind.String } e => Parse(e.GetString()),
            _ => null,
        };

    private static decimal? Parse(string? value) =>
        decimal.TryParse(
            value?.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var result
        )
            ? result
            : null;

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"^\s*(?<amount>\d+(?:[.,]\d+)?)\s*(?<unit>[a-zA-Z]+)\b")]
    private static partial Regex QuantityPattern();
}
