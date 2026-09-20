using HomeBase.Database;
using HomeBase.Database.Entities;
using HomeBase.Database.Enums;
using HomeBase.Database.Queries;
using HomeBase.Features.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HomeBase.Features.MealPlan;

public sealed class MealPlanService(IDbContextFactory<HomeBaseDbContext> factory)
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

        await SetStatusAsync(db, entryId, status, ct);
    }

    private static async Task SetStatusAsync(
        HomeBaseDbContext db,
        long entryId,
        MealPlanStatus status,
        CancellationToken ct
    ) =>
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

        var ingredients = await db
            .RecipeIngredients.ForRecipe(recipeId)
            .InRecipeOrder()
            .Select(IngredientLine.Projection)
            .ToListAsync(ct);

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return CookPlanBuilder.Build(
            entry.Id,
            recipe.Name,
            entry.Servings,
            recipe.Servings,
            ingredients,
            totals
        );
    }

    public async Task<CookResult> CookAsync(
        long entryId,
        IReadOnlyList<CookLine> lines,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        List<CookLine> shortfalls = [];

        foreach (var line in lines.Where(l => l.Needed > 0))
        {
            var result = await InventoryService.TakeFromProductAsync(
                db,
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

        await SetStatusAsync(db, entryId, MealPlanStatus.Cooked, ct);

        await transaction.CommitAsync(ct);

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

        return await GetNeedsAsync(db, from, to, ct);
    }

    private static async Task<IReadOnlyList<NeedRow>> GetNeedsAsync(
        HomeBaseDbContext db,
        DateOnly from,
        DateOnly to,
        CancellationToken ct
    )
    {
        var meals = await db
            .MealPlanEntries.Between(from, to)
            .Planned()
            .Where(e => e.RecipeId != null)
            .Select(e => new PlannedMeal(e.RecipeId!.Value, e.Servings, e.Recipe!.Servings))
            .ToListAsync(ct);

        if (meals.Count == 0)
        {
            return [];
        }

        var recipeIds = meals.Select(m => m.RecipeId).Distinct().ToList();

        var ingredients = await db
            .RecipeIngredients.Where(i => recipeIds.Contains(i.RecipeId))
            .Select(IngredientLine.Projection)
            .ToListAsync(ct);

        var totals = await db.StockLots.StockTotalsByProductAsync(ct);

        return MealNeedsBuilder.Build(meals, ingredients, totals);
    }

    public async Task<int> ApplyToShoppingListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default
    )
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var needs = await GetNeedsAsync(db, from, to, ct);

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

        await transaction.CommitAsync(ct);

        return added;
    }
}
