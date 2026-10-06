namespace HomeBase.Features.Common;

public static class TimeProviderExtensions
{
    // The household's calendar day, for best-before and service dates.
    public static DateOnly Today(this TimeProvider time) =>
        DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
