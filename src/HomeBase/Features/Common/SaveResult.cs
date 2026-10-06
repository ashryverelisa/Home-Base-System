namespace HomeBase.Features.Common;

// Outcome of a write the user started: either done, or a localized message to show.
public sealed record SaveResult(bool Succeeded, string? Error)
{
    private static readonly SaveResult Success = new(true, null);

    public static SaveResult Ok() => Success;

    public static SaveResult<TId> Ok<TId>(TId id)
        where TId : struct => new(true, id, null);

    public static SaveResult Failed(string error) => new(false, error);

    public static SaveResult<TId> Failed<TId>(string error)
        where TId : struct => new(false, default, error);
}

// Same outcome for writes that create something the caller navigates to next.
public sealed record SaveResult<TId>(bool Succeeded, TId Id, string? Error)
    where TId : struct;
