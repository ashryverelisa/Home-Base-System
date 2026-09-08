using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class ShoppingList
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ShoppingListKind Kind { get; set; } = ShoppingListKind.Other;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool Archived { get; set; }
    public List<ShoppingListItem> Items { get; set; } = [];
}
