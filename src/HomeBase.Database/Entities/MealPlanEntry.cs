using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class MealPlanEntry
{
    public long Id { get; set; }
    public DateOnly PlanDate { get; set; }
    public MealSlot Slot { get; set; }
    public int? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
    public string? FreeText { get; set; }
    public int Servings { get; set; } = 2;
    public MealPlanStatus Status { get; set; } = MealPlanStatus.Planned;
    public DateTimeOffset? CookedAt { get; set; }
    public List<StockMovement> Movements { get; set; } = [];
}
