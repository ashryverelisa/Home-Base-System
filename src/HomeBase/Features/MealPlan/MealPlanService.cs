using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.MealPlan;

public sealed class MealPlanService(
    IDbContextFactory<HomeBaseDbContext> factory,
    InventoryService inventory
)
{
    public async Task<IReadOnlyList<MealPlanRow>> GetRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .MealPlanEntries.Between(from, to)
            .InPlanOrder()
            .Select(MealPlanRow.Projection)
            .ToListAsync(ct);
    }

    public async Task<long> AddAsync(
        DateOnly date,
        MealSlot slot,
        int? recipeId,
        string? freeText,
        int servings,
        MealPlanStatus status = MealPlanStatus.Planned,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var entry = new MealPlanEntry
        {
            PlanDate = date,
            Slot = slot,
            RecipeId = recipeId,
            FreeText = recipeId is null ? freeText?.Trim() : null,
            Servings = servings > 0 ? servings : 1,
            Status = status,
            CookedAt = status == MealPlanStatus.Cooked ? DateTimeOffset.UtcNow : null,
        };

        db.MealPlanEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return entry.Id;
    }

    public async Task RemoveAsync(long entryId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db.MealPlanEntries.Where(e => e.Id == entryId).ExecuteDeleteAsync(ct);
    }

    public async Task SetStatusAsync(
        long entryId,
        MealPlanStatus status,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        await db
            .MealPlanEntries.Where(e => e.Id == entryId)
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(e => e.Status, status)
                        .SetProperty(
                            e => e.CookedAt,
                            status == MealPlanStatus.Cooked ? DateTimeOffset.UtcNow : null
                        ),
                ct
            );
    }

    public async Task<CookPlan?> GetCookPlanAsync(long entryId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var entry = await db
            .MealPlanEntries.Include(mealPlanEntry => mealPlanEntry.Recipe)
            .WithRecipe()
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);

        if (entry is null)
        {
            return null;
        }

        if (entry.RecipeId is not { } recipeId || entry.Recipe is not { } recipe)
        {
            return new CookPlan(entry.Id, entry.FreeText ?? string.Empty, entry.Servings, [], []);
        }

        var factorNumerator = entry.Servings;
        var factorDenominator = recipe.Servings > 0 ? recipe.Servings : 1;

        var ingredients = await db
            .RecipeIngredients.ForRecipe(recipeId)
            .InRecipeOrder()
            .Select(i => new
            {
                i.ProductId,
                ProductName = i.Product!.Name,
                i.Product!.BaseUnit,
                i.QuantityBase,
                i.FreeText,
                i.IsOptional,
            })
            .ToListAsync(ct);

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        List<CookLine> lines = [];
        List<string> untracked = [];

        foreach (var ingredient in ingredients)
        {
            if (
                ingredient.ProductId is not { } productId
                || ingredient.QuantityBase is not { } quantity
                || quantity <= 0
            )
            {
                untracked.Add(ingredient.ProductName ?? ingredient.FreeText ?? string.Empty);

                continue;
            }

            var needed = Scale(quantity, factorNumerator, factorDenominator);

            lines.Add(
                new CookLine(
                    productId,
                    ingredient.ProductName!,
                    ingredient.BaseUnit,
                    needed,
                    totals.GetValueOrDefault(productId),
                    ingredient.IsOptional
                )
            );
        }

        return new CookPlan(entry.Id, recipe.Name, entry.Servings, lines, untracked);
    }

    public async Task<CookResult> CookAsync(
        long entryId,
        IReadOnlyList<CookLine> lines,
        CancellationToken ct = default
    )
    {
        List<CookLine> shortfalls = [];

        foreach (var line in lines.Where(l => l.Needed > 0))
        {
            var result = await inventory.TakeFromProductAsync(
                line.ProductId,
                line.Needed,
                StockMovementType.Consume,
                reason: null,
                note: null,
                mealPlanEntryId: entryId,
                ct: ct
            );

            if (!result.IsComplete)
            {
                shortfalls.Add(line with { Available = line.Needed - result.Shortfall });
            }
        }

        await SetStatusAsync(entryId, MealPlanStatus.Cooked, ct);

        return new CookResult(shortfalls.Count == 0, shortfalls);
    }

    public async Task<decimal> GetCostAsync(long entryId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        return await db
            .StockMovements.Where(m => m.MealPlanEntryId == entryId)
            .SumAsync(m => -m.QuantityDelta * (m.Lot!.PurchaseItem!.PricePerBaseUnit ?? 0m), ct);
    }

    public async Task<IReadOnlyList<NeedRow>> GetNeedsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var entries = await db
            .MealPlanEntries.Between(from, to)
            .Planned()
            .Where(e => e.RecipeId != null)
            .Select(e => new
            {
                e.RecipeId,
                e.Servings,
                RecipeServings = e.Recipe!.Servings,
            })
            .ToListAsync(ct);

        if (entries.Count == 0)
        {
            return [];
        }

        var recipeIds = entries.Select(e => e.RecipeId!.Value).Distinct().ToList();

        var ingredients = await db
            .RecipeIngredients.Where(i => recipeIds.Contains(i.RecipeId))
            .Select(i => new
            {
                i.RecipeId,
                i.ProductId,
                ProductName = i.Product!.Name,
                i.Product!.BaseUnit,
                i.QuantityBase,
                i.FreeText,
                i.IsOptional,
            })
            .ToListAsync(ct);

        Dictionary<int, Accumulated> tracked = [];
        Dictionary<string, decimal> free = [];

        foreach (var entry in entries)
        {
            var denominator = entry.RecipeServings > 0 ? entry.RecipeServings : 1;

            foreach (var ingredient in ingredients.Where(i => i.RecipeId == entry.RecipeId))
            {
                if (ingredient.IsOptional)
                {
                    continue;
                }

                if (
                    ingredient.ProductId is not { } productId
                    || ingredient.QuantityBase is not { } quantity
                    || quantity <= 0
                )
                {
                    var label = ingredient.ProductName ?? ingredient.FreeText;

                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        free[label] = free.GetValueOrDefault(label) + 1;
                    }

                    continue;
                }

                var needed = Scale(quantity, entry.Servings, denominator);
                var current = tracked.GetValueOrDefault(
                    productId,
                    new Accumulated(ingredient.ProductName!, ingredient.BaseUnit, 0m)
                );

                tracked[productId] = current with { Needed = current.Needed + needed };
            }
        }

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return
        [
            .. tracked
                .Select(t => new NeedRow(
                    t.Key,
                    t.Value.Name,
                    t.Value.Unit,
                    t.Value.Needed,
                    totals.GetValueOrDefault(t.Key)
                ))
                .OrderByDescending(n => n.Missing)
                .ThenBy(n => n.Label),
            .. free.Select(f => new NeedRow(null, f.Key, null, 0m, 0m)).OrderBy(n => n.Label),
        ];
    }

    public async Task<int> ApplyToShoppingListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        var needs = await GetNeedsAsync(from, to, ct);

        await using var db = await factory.CreateDbContextAsync(ct);

        var list =
            await db.ShoppingLists.Active().FirstOrDefaultAsync(l => l.IsDefault, ct)
            ?? await db.ShoppingLists.Active().InDisplayOrder().FirstOrDefaultAsync(ct);

        if (list is null)
        {
            return 0;
        }

        await db
            .ShoppingListItems.OnList(list.Id)
            .Open()
            .Where(i => i.AddedBy == ShoppingListItemOrigin.MealPlan)
            .ExecuteDeleteAsync(ct);

        var added = 0;

        foreach (var need in needs)
        {
            if (need.ProductId is { } productId)
            {
                if (need.Missing <= 0)
                {
                    continue;
                }

                db.ShoppingListItems.Add(
                    new ShoppingListItem
                    {
                        ListId = list.Id,
                        ProductId = productId,
                        Quantity = need.Missing,
                        AddedBy = ShoppingListItemOrigin.MealPlan,
                    }
                );
            }
            else
            {
                db.ShoppingListItems.Add(
                    new ShoppingListItem
                    {
                        ListId = list.Id,
                        FreeText = need.Label,
                        AddedBy = ShoppingListItemOrigin.MealPlan,
                    }
                );
            }

            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return added;
    }

    private static decimal Scale(decimal quantity, int servings, int recipeServings) =>
        quantity * servings / recipeServings;

    private sealed record Accumulated(string Name, BaseUnit Unit, decimal Needed);
}
