using HomeBase.Features.Shopping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HomeBase.Tests.Shopping;

public sealed class AutoRestockServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IShoppingService _shopping = Substitute.For<IShoppingService>();

    private readonly TaskCompletionSource _ran = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    private async Task<AutoRestockService> RunOnceAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var provider = new ServiceCollection()
            .AddScoped(_ => _shopping)
            .BuildServiceProvider();

        var service = new AutoRestockService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AutoRestockService>.Instance
        );

        await service.StartAsync(ct);
        await _ran.Task.WaitAsync(Timeout, ct);
        await service.StopAsync(ct);

        return service;
    }

    [Fact]
    public async Task Start_RunsRestockOnceRightAway()
    {
        _shopping
            .RunAutoRestockAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _ran.TrySetResult();

                return 2;
            });

        await RunOnceAsync();

        await _shopping.Received(1).RunAutoRestockAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_RestockThrows_SwallowsErrorAndStopsCleanly()
    {
        _shopping
            .RunAutoRestockAsync(Arg.Any<CancellationToken>())
            .Returns<int>(_ =>
            {
                _ran.TrySetResult();

                throw new InvalidOperationException("database down");
            });

        var service = await RunOnceAsync();

        Assert.True(service.ExecuteTask?.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Stop_CancelsTokenPassedToRestock()
    {
        var passed = CancellationToken.None;

        _shopping
            .RunAutoRestockAsync(Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                passed = call.Arg<CancellationToken>();
                _ran.TrySetResult();

                return 0;
            });

        await RunOnceAsync();

        Assert.True(passed.IsCancellationRequested);
    }
}
