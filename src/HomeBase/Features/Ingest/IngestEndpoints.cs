using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Inventory;
using HomeBase.Features.Purchases;
using HomeBase.Features.Shopping;

namespace HomeBase.Features.Ingest;

public static class IngestEndpoints
{
    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1")
            .AddEndpointFilter<ApiKeyFilter>()
            .RequireRateLimiting(ApiKeyFilter.RateLimitPolicy);

        group.MapPost("/receipts", PostReceiptAsync);
        group.MapGet("/receipts", GetReceiptsAsync);
        group.MapPost("/receipts/{id:long}/confirm", ConfirmReceiptAsync);
        group.MapGet("/products", GetProductsAsync);
        group.MapPost("/shopping-lists/{id:int}/items", PostShoppingItemAsync);
        group.MapGet("/stock/low", GetLowStockAsync);
        group.MapGet("/stock/expiring", GetExpiringAsync);
        group.MapPost("/recipes", PostRecipeAsync);

        return endpoints;
    }

    private static async Task<IResult> PostReceiptAsync(
        ReceiptRequest request,
        ReceiptIngestService ingest,
        HttpContext http,
        CancellationToken ct
    )
    {
        var result = await ingest.IngestAsync(request, ct);

        if (result is null)
        {
            return Results.BadRequest(new { error = "The receipt has no items." });
        }

        var response = new ReceiptResponse(
            result.PurchaseId,
            result.Status.ToString(),
            result.Matched,
            result.Unmatched,
            ReviewUrl(http, result.PurchaseId)
        );

        return result.WasKnown
            ? Results.Ok(response)
            : Results.Created($"/api/v1/receipts/{result.PurchaseId}", response);
    }

    private static async Task<IResult> PostRecipeAsync(
        RecipeRequest request,
        RecipeIngestService recipes,
        CancellationToken ct
    )
    {
        var result = await recipes.IngestAsync(request, ct);

        if (result is null)
        {
            return Results.BadRequest(new { error = "The recipe needs a name." });
        }

        var response = new
        {
            recipeId = result.RecipeId,
            matched = result.Matched,
            unmatched = result.Unmatched,
        };

        return result.WasKnown
            ? Results.Ok(response)
            : Results.Created($"/recipes/{result.RecipeId}", response);
    }

    private static async Task<IResult> GetReceiptsAsync(
        string? status,
        PurchaseService purchases,
        CancellationToken ct
    )
    {
        var pending = await purchases.GetPendingAsync(ct);

        if (status is { Length: > 0 } && Enum.TryParse<PurchaseStatus>(status, true, out var wanted))
        {
            pending = [.. pending.Where(p => p.Status == wanted)];
        }

        var summaries = new List<ReceiptSummary>(pending.Count);

        foreach (var row in pending)
        {
            var lines = await purchases.GetLinesAsync(row.Id, ct);

            summaries.Add(
                new ReceiptSummary(
                    row.Id,
                    row.Status.ToString(),
                    row.PurchasedAt,
                    row.StoreName,
                    row.Total,
                    lines.Count,
                    lines.Count(l =>
                        l.LineType == PurchaseLineType.Item && l.ProductId is null
                    )
                )
            );
        }

        return Results.Ok(summaries);
    }

    private static async Task<IResult> ConfirmReceiptAsync(
        long id,
        PurchaseService purchases,
        CancellationToken ct
    )
    {
        var purchase = await purchases.FindAsync(id, ct);

        if (purchase is null)
        {
            return Results.NotFound();
        }

        var confirmed = await purchases.ConfirmAsync(id, ct);

        return Results.Ok(
            new
            {
                purchaseId = id,
                status = PurchaseStatus.Confirmed.ToString(),
                booked = confirmed,
            }
        );
    }

    private static async Task<IResult> GetProductsAsync(
        string? q,
        string? gtin,
        CatalogService catalog,
        CancellationToken ct
    )
    {
        if (gtin is { Length: > 0 })
        {
            var matches = await catalog.SearchAsync(gtin, ct);
            var product = matches.FirstOrDefault(r => r.Gtin == gtin.Trim());

            return product is null
                ? Results.NotFound()
                : Results.Ok(
                    new ProductSummary(
                        product.Id,
                        product.Name,
                        product.Brand,
                        product.Gtin,
                        product.BaseUnit.ToString(),
                        product.PackageSize,
                        product.StockBase
                    )
                );
        }

        var rows = await catalog.SearchAsync(q, ct);

        return Results.Ok(
            rows.Select(r => new ProductSummary(
                    r.Id,
                    r.Name,
                    r.Brand,
                    r.Gtin,
                    r.BaseUnit.ToString(),
                    r.PackageSize,
                    r.StockBase
                ))
                .Take(50)
        );
    }

    private static async Task<IResult> PostShoppingItemAsync(
        int id,
        ShoppingItemRequest request,
        ShoppingService shopping,
        CancellationToken ct
    )
    {
        var result = request switch
        {
            { ProductId: { } productId } => await shopping.AddProductAsync(
                new AddProductRequest(
                    id,
                    productId,
                    request.Quantity,
                    request.Note,
                    ShoppingListItemOrigin.Api
                ),
                ct
            ),
            { Text: { Length: > 0 } text } => await shopping.AddFreeTextAsync(
                new AddFreeTextRequest(
                    id,
                    text,
                    request.Quantity,
                    request.Unit,
                    null,
                    request.Note,
                    ShoppingListItemOrigin.Api
                ),
                ct
            ),
            _ => ShoppingSaveResult.Failed("Either productId or text is required."),
        };

        return result.Succeeded
            ? Results.Created($"/api/v1/shopping-lists/{id}/items/{result.ItemId}", new { itemId = result.ItemId })
            : Results.BadRequest(new { error = result.Error });
    }

    private static async Task<IResult> GetLowStockAsync(
        InventoryService inventory,
        CancellationToken ct
    )
    {
        var rows = await inventory.GetLowStockAsync(ct);

        return Results.Ok(
            rows.Select(r => new LowStockResponse(
                r.ProductId,
                r.Name,
                r.BaseUnit.ToString(),
                r.StockBase,
                r.MinStockBase
            ))
        );
    }

    private static async Task<IResult> GetExpiringAsync(
        InventoryService inventory,
        CancellationToken ct,
        int days = 3
    )
    {
        var lots = await inventory.GetStockAsync(withinDays: days, ct: ct);

        return Results.Ok(
            lots.Select(l => new ExpiringResponse(
                l.LotId,
                l.ProductId,
                l.ProductName,
                l.BaseUnit.ToString(),
                l.QuantityBase,
                l.BestBefore,
                l.DaysLeft,
                l.LocationName
            ))
        );
    }

    private static string ReviewUrl(HttpContext http, long purchaseId) =>
        $"{http.Request.Scheme}://{http.Request.Host}/purchases/{purchaseId}/review";
}
