namespace HomeBase.Features.Common;

public static class ShelfLife
{
    public static DateOnly? Resolve(
        DateOnly? enteredBestBefore,
        int? defaultShelfLifeDays,
        DateOnly bookedOn
    ) => enteredBestBefore ?? (defaultShelfLifeDays is { } days ? bookedOn.AddDays(days) : null);

    public static int? Learnable(
        DateOnly? enteredBestBefore,
        int? defaultShelfLifeDays,
        DateOnly bookedOn
    ) =>
        defaultShelfLifeDays is null && enteredBestBefore is { } date && date >= bookedOn
            ? date.DayNumber - bookedOn.DayNumber
            : null;
}
