using HomeBase.Database.Enums;

namespace HomeBase.Features.Recipes;

public sealed record RecipeSaveResult(bool Succeeded, int RecipeId, string? Error)
{
    public static RecipeSaveResult Ok(int recipeId) => new(true, recipeId, null);

    public static RecipeSaveResult Failed(string error) => new(false, 0, error);
}

public sealed record IngredientLine(
    int Id,
    int? ProductId,
    string? ProductName,
    string? FreeText,
    BaseUnit? BaseUnit,
    decimal? QuantityBase,
    bool IsOptional,
    string? Note
)
{
    public string Label => ProductName ?? FreeText ?? string.Empty;

    public bool IsTracked => ProductId is not null && QuantityBase is > 0;
}
