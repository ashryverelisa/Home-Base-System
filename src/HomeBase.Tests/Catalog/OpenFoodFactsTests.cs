using System.Net;
using System.Text;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeBase.Tests.Catalog;

public class OpenFoodFactsTests
{
    private const string Nutella = "3017624010701";

    [Theory]
    [InlineData(400, "g", BaseUnit.Gram, 400)]
    [InlineData(1.5, "kg", BaseUnit.Gram, 1500)]
    [InlineData(500, "ml", BaseUnit.Milliliter, 500)]
    [InlineData(33, "cl", BaseUnit.Milliliter, 330)]
    [InlineData(1, "L", BaseUnit.Milliliter, 1000)]
    public void PackageFrom_AmountAndUnit_ConvertsToBaseUnit(
        double amount,
        string unit,
        BaseUnit expectedUnit,
        double expectedSize
    )
    {
        var package = OpenFoodFactsMapping.PackageFrom((decimal)amount, unit);

        Assert.Equal((expectedUnit, (decimal)expectedSize), package);
    }

    [Theory]
    [InlineData(null, "g")]
    [InlineData(0, "g")]
    [InlineData(6, "pieces")]
    [InlineData(400, null)]
    public void PackageFrom_MissingOrUnknown_ReturnsNull(int? amount, string? unit)
    {
        Assert.Null(OpenFoodFactsMapping.PackageFrom((decimal?)amount, unit));
    }

    [Theory]
    [InlineData("400.0 g", BaseUnit.Gram, 400)]
    [InlineData("0,5 l", BaseUnit.Milliliter, 500)]
    [InlineData("250g", BaseUnit.Gram, 250)]
    public void PackageFrom_QuantityText_IsParsed(
        string text,
        BaseUnit expectedUnit,
        double expectedSize
    )
    {
        Assert.Equal((expectedUnit, (decimal)expectedSize), OpenFoodFactsMapping.PackageFrom(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("6 x 0,5 l")]
    [InlineData("ein Glas")]
    public void PackageFrom_UnparseableText_ReturnsNull(string? text)
    {
        Assert.Null(OpenFoodFactsMapping.PackageFrom(text));
    }

    [Fact]
    public async Task LookupAsync_Found_MapsProduct()
    {
        var client = Client(
            HttpStatusCode.OK,
            """
            {"status":1,"product":{"product_name":"Nutella","product_name_de":"Nutella DE",
             "brands":"Ferrero, Ferrero Deutschland","product_quantity":400,
             "product_quantity_unit":"g","image_front_url":"https://img/front.jpg"}}
            """
        );

        var lookup = await client.LookupAsync(Nutella, TestContext.Current.CancellationToken);

        Assert.Equal(OffLookupStatus.Found, lookup.Status);
        Assert.Equal(
            new OffProduct(Nutella, "Nutella DE", "Ferrero", BaseUnit.Gram, 400m, "https://img/front.jpg"),
            lookup.Product
        );
    }

    [Fact]
    public async Task LookupAsync_QuantityAsStringWithoutUnit_FallsBackToQuantityText()
    {
        var client = Client(
            HttpStatusCode.OK,
            """{"status":1,"product":{"product_name":"Milch","product_quantity":"","quantity":"1 l"}}"""
        );

        var lookup = await client.LookupAsync(Nutella, TestContext.Current.CancellationToken);

        Assert.Equal(BaseUnit.Milliliter, lookup.Product?.BaseUnit);
        Assert.Equal(1000m, lookup.Product?.PackageSize);
    }

    [Fact]
    public async Task LookupAsync_NotFound_ReportsNotFound()
    {
        var client = Client(
            HttpStatusCode.NotFound,
            """{"status":0,"status_verbose":"product not found"}"""
        );

        var lookup = await client.LookupAsync(Nutella, TestContext.Current.CancellationToken);

        Assert.Equal(OffLookupStatus.NotFound, lookup.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "")]
    [InlineData(HttpStatusCode.OK, "<html>not json</html>")]
    public async Task LookupAsync_ServerErrorOrGarbage_ReportsUnavailable(
        HttpStatusCode status,
        string body
    )
    {
        var lookup = await Client(status, body)
            .LookupAsync(Nutella, TestContext.Current.CancellationToken);

        Assert.Equal(OffLookupStatus.Unavailable, lookup.Status);
    }

    [Fact]
    public void ApplyTo_NewProduct_FillsEverything()
    {
        var product = new Product { Name = string.Empty, IsFood = true };

        Off().ApplyTo(product);

        Assert.Equal("Nutella", product.Name);
        Assert.Equal("Ferrero", product.Brand);
        Assert.Equal(BaseUnit.Gram, product.BaseUnit);
        Assert.Equal(400m, product.PackageSize);
        Assert.Equal("https://img/front.jpg", product.ImageUrl);
        Assert.Equal(Nutella, product.OffId);
    }

    [Fact]
    public void ApplyTo_MaintainedProduct_KeepsExistingValues()
    {
        var product = new Product
        {
            Name = "Nuss-Nougat-Creme",
            Brand = "Eigenmarke",
            BaseUnit = BaseUnit.Gram,
            PackageSize = 450m,
            ImageUrl = "https://own/image.jpg",
        };

        Off().ApplyTo(product);

        Assert.Equal("Nuss-Nougat-Creme", product.Name);
        Assert.Equal("Eigenmarke", product.Brand);
        Assert.Equal(450m, product.PackageSize);
        Assert.Equal("https://own/image.jpg", product.ImageUrl);
        Assert.Equal(Nutella, product.OffId);
    }

    [Fact]
    public void HasDetails_EmptyEntry_IsFalse()
    {
        Assert.False(new OffProduct(Nutella, null, null, BaseUnit.Piece, 1m, null).HasDetails);
    }

    private static OffProduct Off() =>
        new(Nutella, "Nutella", "Ferrero", BaseUnit.Gram, 400m, "https://img/front.jpg");

    private static OpenFoodFactsClient Client(HttpStatusCode status, string body) =>
        new(
            new HttpClient(new StubHandler(status, body))
            {
                BaseAddress = new Uri("https://off.test/"),
            },
            NullLogger<OpenFoodFactsClient>.Instance
        );

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
    }
}
