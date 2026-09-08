namespace HomeBase.Database.Entities;

public class Recipe
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Servings { get; set; } = 2;
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }
    public string? Instructions { get; set; }
    public string? SourceUrl { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; } = [];
}
