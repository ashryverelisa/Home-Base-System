using System.Linq.Expressions;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;

namespace HomeBase.Features.Recipes;

// An ingredient as the recipe editor shows it; the meal plan reads ingredients as IngredientLine.
public sealed record RecipeIngredientRow(
    int Id,
    int? ProductId,
    string? ProductName,
    string? FreeText,
    BaseUnit? BaseUnit,
    decimal? QuantityBase,
    bool IsOptional,
    string? Note,
    bool NeedsReview
)
{
    public static readonly Expression<Func<RecipeIngredient, RecipeIngredientRow>> Projection =
        ingredient => new RecipeIngredientRow(
            ingredient.Id,
            ingredient.ProductId,
            ingredient.Product!.Name,
            ingredient.FreeText,
            ingredient.Product!.BaseUnit,
            ingredient.QuantityBase,
            ingredient.IsOptional,
            ingredient.Note,
            ingredient.NeedsReview
        );

    public string Label => ProductName ?? FreeText ?? string.Empty;

    public bool IsTracked => ProductId is not null && QuantityBase is > 0;
}

public sealed record IngredientReview(
    int IngredientId,
    string Text,
    string Name,
    ProductRow? Suggestion
);
