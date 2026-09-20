namespace HomeBase.Features.Shopping;

public sealed partial class AutoRestockService(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoRestockService> logger
) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Auto restock put {Count} product(s) on a shopping list"
    )]
    private static partial void LogRestocked(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Auto restock failed")]
    private static partial void LogFailed(ILogger logger, Exception exception);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                await RunAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Shutdown, not a failure - without this the host logs the stop as an error.
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var shopping = scope.ServiceProvider.GetRequiredService<ShoppingService>();
            var added = await shopping.RunAutoRestockAsync(ct);

            if (added > 0)
            {
                LogRestocked(logger, added);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // shutting down
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception);
        }
    }
}
