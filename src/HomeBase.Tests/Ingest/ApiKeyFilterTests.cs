using HomeBase.Features.Ingest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HomeBase.Tests.Ingest;

public class ApiKeyFilterTests
{
    private const string Key = "s3cret-key";

    private static readonly object NextResult = new();

    private static async Task<(object? Result, bool NextCalled)> InvokeAsync(
        string? configuredKey,
        string? providedKey
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [ApiKeyFilter.ConfigurationKey] = configuredKey }
            )
            .Build();

        var http = new DefaultHttpContext();

        if (providedKey is not null)
        {
            http.Request.Headers[ApiKeyFilter.HeaderName] = providedKey;
        }

        var nextCalled = false;
        var filter = new ApiKeyFilter(configuration);

        var result = await filter.InvokeAsync(
            new DefaultEndpointFilterInvocationContext(http),
            _ =>
            {
                nextCalled = true;

                return ValueTask.FromResult<object?>(NextResult);
            }
        );

        return (result, nextCalled);
    }

    [Fact]
    public async Task MatchingKey_CallsEndpoint()
    {
        var (result, nextCalled) = await InvokeAsync(Key, Key);

        Assert.True(nextCalled);
        Assert.Same(NextResult, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData("s3cret-key ")]
    [InlineData("S3CRET-KEY")]
    public async Task MissingOrWrongKey_IsUnauthorized(string? provided)
    {
        var (result, nextCalled) = await InvokeAsync(Key, provided);

        Assert.False(nextCalled);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false).StatusCode
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task KeyNotConfigured_IsServiceUnavailableEvenWithEmptyHeader(string? configured)
    {
        var (result, nextCalled) = await InvokeAsync(configured, "");

        Assert.False(nextCalled);
        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false).StatusCode
        );
    }
}
