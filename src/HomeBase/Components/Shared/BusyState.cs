namespace HomeBase.Components.Shared;

// Disables a page's buttons while a write runs, and re-enables them even when it throws.
public sealed class BusyState
{
    public bool IsBusy { get; private set; }

    public async Task RunAsync(Func<Task> action)
    {
        IsBusy = true;

        try
        {
            await action();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
