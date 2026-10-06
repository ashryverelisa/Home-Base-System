namespace HomeBase.Features.Shopping;

public enum ShoppingPriority
{
    Low = -1,
    Normal = 0,
    High = 1,
    Urgent = 2,
}

public static class ShoppingPriorities
{
    public static readonly IReadOnlyList<ShoppingPriority> All =
    [
        ShoppingPriority.Urgent,
        ShoppingPriority.High,
        ShoppingPriority.Normal,
        ShoppingPriority.Low,
    ];

    public static ShoppingPriority FromValue(int value) =>
        (ShoppingPriority)
            Math.Clamp(value, (int)ShoppingPriority.Low, (int)ShoppingPriority.Urgent);
}
