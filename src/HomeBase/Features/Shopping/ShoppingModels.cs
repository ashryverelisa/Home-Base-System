using HomeBase.Database.Enums;

namespace HomeBase.Features.Shopping;

public sealed record AddProductRequest(
    int ListId,
    int ProductId,
    decimal? Quantity = null,
    string? Note = null,
    ShoppingListItemOrigin Origin = ShoppingListItemOrigin.User
);

public sealed record AddFreeTextRequest(
    int ListId,
    string Text,
    decimal? Quantity = null,
    string? Unit = null,
    decimal? TargetPrice = null,
    string? Note = null,
    ShoppingListItemOrigin Origin = ShoppingListItemOrigin.User
);

public sealed record ShoppingSaveResult(bool Succeeded, long ItemId, string? Error)
{
    public static ShoppingSaveResult Ok(long itemId) => new(true, itemId, null);

    public static ShoppingSaveResult Failed(string error) => new(false, 0, error);
}
