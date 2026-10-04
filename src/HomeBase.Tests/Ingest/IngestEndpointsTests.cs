using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Ingest;
using HomeBase.Features.Inventory;
using HomeBase.Features.Purchases;
using HomeBase.Features.Shopping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;

namespace HomeBase.Tests.Ingest;

public class IngestEndpointsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static int StatusCode(IResult result) =>
        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode ?? 0;

    private static ReceiptRequest Receipt() =>
        new(null, null, null, null, null, null, null, [], null);

    private static PurchaseRow Purchase(long id, PurchaseStatus status) =>
        new(id, DateTimeOffset.UnixEpoch, null, "Rewe", 12.5m, PurchaseSource.N8nReceipt, status);

    private static PurchaseLineRow Line(PurchaseLineType type, int? productId) =>
        new(1, 1, type, productId, null, null, null, 1m, null, 1m, null, false);

    private static ProductRow Product(int id, string gtin) =>
        new(id, "Milch", null, null, BaseUnit.Milliliter, 1000m, true, gtin, null);

    [Fact]
    public async Task PostReceipt_NoItems_ReturnsBadRequest()
    {
        var ingest = Substitute.For<IReceiptIngestService>();
        ingest.IngestAsync(Arg.Any<ReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns((ReceiptIngestResult?)null);

        var result = await IngestEndpoints.PostReceiptAsync(
            Receipt(),
            ingest,
            new DefaultHttpContext(),
            Ct
        );

        Assert.Equal(StatusCodes.Status400BadRequest, StatusCode(result));
    }

    [Fact]
    public async Task PostReceipt_NewReceipt_ReturnsCreatedWithReviewUrl()
    {
        var request = Receipt();
        var ingest = Substitute.For<IReceiptIngestService>();
        ingest.IngestAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ReceiptIngestResult(42, PurchaseStatus.NeedsReview, 3, 1, WasKnown: false));

        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("homebase.local");

        var result = await IngestEndpoints.PostReceiptAsync(request, ingest, http, Ct);

        var created = Assert.IsType<Created<ReceiptResponse>>(result);
        Assert.Equal("/api/v1/receipts/42", created.Location);
        Assert.Equal(
            new ReceiptResponse(42, "NeedsReview", 3, 1, "https://homebase.local/purchases/42/review"),
            created.Value
        );
    }

    [Fact]
    public async Task PostReceipt_KnownReceipt_ReturnsOk()
    {
        var ingest = Substitute.For<IReceiptIngestService>();
        ingest.IngestAsync(Arg.Any<ReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiptIngestResult(42, PurchaseStatus.Confirmed, 3, 0, WasKnown: true));

        var result = await IngestEndpoints.PostReceiptAsync(
            Receipt(),
            ingest,
            new DefaultHttpContext(),
            Ct
        );

        Assert.Equal(42, Assert.IsType<Ok<ReceiptResponse>>(result).Value?.PurchaseId);
    }

    [Fact]
    public async Task GetReceipts_StatusFilter_SkipsOtherPurchasesAndCountsUnmatchedItems()
    {
        var purchases = Substitute.For<IPurchaseService>();
        purchases.GetPendingAsync(Arg.Any<CancellationToken>())
            .Returns([Purchase(1, PurchaseStatus.NeedsReview), Purchase(2, PurchaseStatus.Draft)]);
        purchases.GetLinesAsync(1, Arg.Any<CancellationToken>())
            .Returns(
                [
                    Line(PurchaseLineType.Item, productId: 5),
                    Line(PurchaseLineType.Item, productId: null),
                    Line(PurchaseLineType.Discount, productId: null),
                ]
            );

        var result = await IngestEndpoints.GetReceiptsAsync("needsreview", purchases, Ct);

        var summary = Assert.Single(Assert.IsType<Ok<List<ReceiptSummary>>>(result).Value!);
        Assert.Equal(1, summary.PurchaseId);
        Assert.Equal(3, summary.Lines);
        Assert.Equal(1, summary.Unmatched);
        await purchases.DidNotReceive().GetLinesAsync(2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmReceipt_UnknownPurchase_ReturnsNotFoundWithoutConfirming()
    {
        var purchases = Substitute.For<IPurchaseService>();
        purchases.FindAsync(7, Arg.Any<CancellationToken>()).Returns((PurchaseRow?)null);

        var result = await IngestEndpoints.ConfirmReceiptAsync(7, purchases, Ct);

        Assert.IsType<NotFound>(result);
        await purchases.DidNotReceive().ConfirmAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmReceipt_KnownPurchase_ConfirmsIt()
    {
        var purchases = Substitute.For<IPurchaseService>();
        purchases.FindAsync(7, Arg.Any<CancellationToken>())
            .Returns(Purchase(7, PurchaseStatus.NeedsReview));
        purchases.ConfirmAsync(7, Arg.Any<CancellationToken>()).Returns(true);

        var result = await IngestEndpoints.ConfirmReceiptAsync(7, purchases, Ct);

        Assert.Equal(StatusCodes.Status200OK, StatusCode(result));
        await purchases.Received(1).ConfirmAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProducts_ByGtin_ReturnsExactMatchOnly()
    {
        var catalog = Substitute.For<ICatalogService>();
        catalog.SearchAsync("4001", Arg.Any<CancellationToken>())
            .Returns([Product(1, "40012"), Product(2, "4001")]);

        var result = await IngestEndpoints.GetProductsAsync(null, "4001", catalog, Ct);

        Assert.Equal(2, Assert.IsType<Ok<ProductSummary>>(result).Value?.Id);
    }

    [Fact]
    public async Task GetProducts_ByUnknownGtin_ReturnsNotFound()
    {
        var catalog = Substitute.For<ICatalogService>();
        catalog.SearchAsync("4001", Arg.Any<CancellationToken>()).Returns([Product(1, "40012")]);

        var result = await IngestEndpoints.GetProductsAsync(null, "4001", catalog, Ct);

        Assert.IsType<NotFound>(result);
    }

    [Fact]
    public async Task PostShoppingItem_WithProductId_AddsProductAsApiOrigin()
    {
        var shopping = Substitute.For<IShoppingService>();
        shopping.AddProductAsync(Arg.Any<AddProductRequest>(), Arg.Any<CancellationToken>())
            .Returns(ShoppingSaveResult.Ok(99));

        var result = await IngestEndpoints.PostShoppingItemAsync(
            3,
            new ShoppingItemRequest(11, "ignored", 2m, null, "Bio"),
            shopping,
            Ct
        );

        Assert.Equal(StatusCodes.Status201Created, StatusCode(result));
        await shopping
            .Received(1)
            .AddProductAsync(
                new AddProductRequest(3, 11, 2m, "Bio", ShoppingListItemOrigin.Api),
                Arg.Any<CancellationToken>()
            );
        await shopping
            .DidNotReceive()
            .AddFreeTextAsync(Arg.Any<AddFreeTextRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostShoppingItem_WithText_AddsFreeTextAsApiOrigin()
    {
        var shopping = Substitute.For<IShoppingService>();
        shopping.AddFreeTextAsync(Arg.Any<AddFreeTextRequest>(), Arg.Any<CancellationToken>())
            .Returns(ShoppingSaveResult.Ok(99));

        await IngestEndpoints.PostShoppingItemAsync(
            3,
            new ShoppingItemRequest(null, "Milch", 1m, "l", null),
            shopping,
            Ct
        );

        await shopping
            .Received(1)
            .AddFreeTextAsync(
                new AddFreeTextRequest(3, "Milch", 1m, "l", null, null, ShoppingListItemOrigin.Api),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task PostShoppingItem_WithoutProductOrText_ReturnsBadRequestWithoutSaving()
    {
        var shopping = Substitute.For<IShoppingService>();

        var result = await IngestEndpoints.PostShoppingItemAsync(
            3,
            new ShoppingItemRequest(null, "", null, null, null),
            shopping,
            Ct
        );

        Assert.Equal(StatusCodes.Status400BadRequest, StatusCode(result));
        Assert.Empty(shopping.ReceivedCalls());
    }

    [Fact]
    public async Task PostShoppingItem_ServiceFails_ReturnsBadRequest()
    {
        var shopping = Substitute.For<IShoppingService>();
        shopping.AddProductAsync(Arg.Any<AddProductRequest>(), Arg.Any<CancellationToken>())
            .Returns(ShoppingSaveResult.Failed("Unknown product."));

        var result = await IngestEndpoints.PostShoppingItemAsync(
            3,
            new ShoppingItemRequest(11, null, null, null, null),
            shopping,
            Ct
        );

        Assert.Equal(StatusCodes.Status400BadRequest, StatusCode(result));
    }

    [Fact]
    public async Task GetExpiring_QueriesStockWithinRequestedDays()
    {
        var inventory = Substitute.For<IInventoryService>();

        await IngestEndpoints.GetExpiringAsync(inventory, Ct, days: 5);

        await inventory.Received(1).GetStockAsync(null, 5, Arg.Any<CancellationToken>());
    }
}
