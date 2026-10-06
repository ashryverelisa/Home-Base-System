using HomeBase.Components.Shared;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Features.Catalog;
using HomeBase.Features.Common;
using Microsoft.AspNetCore.Components;

namespace HomeBase.Features.Recipes.Pages;

public sealed partial class RecipeEdit
{
    private Recipe? _recipe;
    private IReadOnlyList<RecipeIngredientRow> _ingredients = [];
    private IReadOnlyList<IngredientReview> _reviews = [];
    private readonly Dictionary<int, ProductRow?> _choices = [];
    private ProductRow? _picked;
    private string _ingredientText = string.Empty;
    private string _tagInput = string.Empty;
    private decimal? _quantity;
    private bool _optional;
    private string? _error;
    private readonly BusyState _busy = new();

    [Parameter]
    public int Id { get; set; }

    private string Title =>
        Id == 0 ? Localizer["Recipes.New"] : _recipe?.Name ?? Localizer["Recipes.Title"].Value;

    protected override async Task OnParametersSetAsync()
    {
        _recipe = Id == 0 ? new Recipe { Name = string.Empty } : await Recipes.FindAsync(Id);

        if (_recipe is null)
        {
            return;
        }

        _tagInput = RecipeTags.Format(_recipe.Tags);

        if (Id > 0)
        {
            await LoadIngredientsAsync();
        }
    }

    private async Task LoadIngredientsAsync()
    {
        _ingredients = await Recipes.GetIngredientsAsync(Id);
        _reviews = await Recipes.GetReviewAsync(Id);

        _choices.Clear();

        foreach (var review in _reviews)
        {
            _choices[review.IngredientId] = review.Suggestion;
        }
    }

    private async Task AssignAsync(IngredientReview review)
    {
        if (_choices.GetValueOrDefault(review.IngredientId) is not { } product)
        {
            return;
        }

        await _busy.RunAsync(async () =>
        {
            await Recipes.AssignIngredientAsync(review.IngredientId, product.Id);
            await LoadIngredientsAsync();
        });
    }

    private async Task KeepAsTextAsync(IngredientReview review)
    {
        await _busy.RunAsync(async () =>
        {
            await Recipes.KeepAsTextAsync(review.IngredientId);
            await LoadIngredientsAsync();
        });
    }

    private async Task<IEnumerable<ProductRow>> SearchProductsAsync(
        string? term,
        CancellationToken ct
    ) => await Catalog.SearchAsync(term, ct);

    private void PickProduct(ProductRow? row)
    {
        _picked = row;
        _quantity = null;
    }

    private async Task SaveAsync()
    {
        if (_recipe is null)
        {
            return;
        }

        _error = null;

        await _busy.RunAsync(async () =>
        {
            _recipe.Tags = RecipeTags.Parse(_tagInput);

            var result = await Recipes.SaveAsync(_recipe);

            if (!result.Succeeded)
            {
                _error = result.Error;
                return;
            }

            Navigation.NavigateTo(Id == 0 ? $"/recipes/{result.Id}" : "/recipes");
        });
    }

    private async Task AddIngredientAsync()
    {
        if (_picked is null && string.IsNullOrWhiteSpace(_ingredientText))
        {
            return;
        }

        await Recipes.AddIngredientAsync(
            Id,
            _picked?.Id,
            _picked is null ? _ingredientText : null,
            _picked is null ? null : _quantity,
            _optional
        );

        _picked = null;
        _ingredientText = string.Empty;
        _quantity = null;
        _optional = false;

        await LoadIngredientsAsync();
    }

    private async Task RemoveIngredientAsync(RecipeIngredientRow ingredient)
    {
        await Recipes.RemoveIngredientAsync(ingredient.Id);

        await LoadIngredientsAsync();
    }

    private async Task CookNowAsync()
    {
        if (_recipe is null)
        {
            return;
        }

        var entryId = await Plan.AddAsync(
            Time.Today(),
            MealSlot.Dinner,
            Id,
            null,
            _recipe.Servings
        );

        Navigation.NavigateTo($"/plan?cook={entryId}");
    }
}
