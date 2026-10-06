using System.Text.Json;
using HomeBase.Features.Catalog;

namespace HomeBase.Features.Ingest;

public sealed record ReceiptRequest(
    string? Source,
    string? ExternalId,
    ReceiptStore? Store,
    DateTimeOffset? PurchasedAt,
    string? Currency,
    decimal? Total,
    string? PaymentMethod,
    IReadOnlyList<ReceiptItem>? Items,
    JsonElement? Raw
);

public sealed record ReceiptStore(string? Name, string? TaxId);

public sealed record ReceiptItem(
    int? LineNo,
    string? RawText,
    string? Gtin,
    string? LineType,
    decimal Quantity,
    string? Unit,
    decimal? UnitPrice,
    decimal LineTotal,
    decimal? Discount,
    bool? IsPromo,
    decimal? TaxRate
);

public sealed record ReceiptResponse(
    long PurchaseId,
    string Status,
    int Matched,
    int Unmatched,
    string? ReviewUrl
);

public sealed record ReceiptSummary(
    long PurchaseId,
    string Status,
    DateTimeOffset PurchasedAt,
    string? Store,
    decimal? Total,
    int Lines,
    int Unmatched
);

public sealed record ShoppingItemRequest(
    int? ProductId,
    string? Text,
    decimal? Quantity,
    string? Unit,
    string? Note
);

public sealed record ProductSummary(
    int Id,
    string Name,
    string? Brand,
    string? Gtin,
    string BaseUnit,
    decimal PackageSize,
    decimal StockBase
)
{
    public static ProductSummary From(ProductRow row) =>
        new(
            row.Id,
            row.Name,
            row.Brand,
            row.Gtin,
            row.BaseUnit.ToString(),
            row.PackageSize,
            row.StockBase
        );
}

public sealed record LowStockResponse(
    int ProductId,
    string Name,
    string BaseUnit,
    decimal StockBase,
    decimal MinStockBase
);

public sealed record ExpiringResponse(
    long LotId,
    int ProductId,
    string Name,
    string BaseUnit,
    decimal QuantityBase,
    DateOnly? BestBefore,
    int? DaysLeft,
    string? Location
);
