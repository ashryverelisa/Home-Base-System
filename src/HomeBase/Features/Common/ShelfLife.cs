namespace HomeBase.Features.Common;

public static class ShelfLife
{
    public static DateOnly? Resolve(
        DateOnly? enteredBestBefore,
        int? defaultShelfLifeDays,
        DateOnly bookedOn
    ) => enteredBestBefore ?? (defaultShelfLifeDays is { } days ? bookedOn.AddDays(days) : null);
}
