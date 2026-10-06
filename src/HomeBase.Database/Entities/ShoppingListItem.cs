using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class ShoppingListItem
{
    public long Id { get; set; }
    public int ListId { get; set; }
    public ShoppingList? List { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? FreeText { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public int Priority { get; set; }
    public decimal? TargetPrice { get; set; }
    public ShoppingListItemStatus Status { get; set; } = ShoppingListItemStatus.Open;
    public ShoppingListItemOrigin AddedBy { get; set; } = ShoppingListItemOrigin.User;
    public string? Note { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public DateTimeOffset? BoughtAt { get; set; }
    public long? PurchaseItemId { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
    public DateOnly? PlanFrom { get; set; }
    public DateOnly? PlanTo { get; set; }
}
