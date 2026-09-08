using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class Category
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];
    public required string Name { get; set; }
    public CategoryKind Kind { get; set; } = CategoryKind.Other;
    public string? Icon { get; set; }
}
