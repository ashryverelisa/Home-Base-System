namespace HomeBase.Database.Entities;

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe? Recipe { get; set; }
    public int SortOrder { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? FreeText { get; set; }
    public decimal? QuantityBase { get; set; }
    public bool IsOptional { get; set; }
    public string? Note { get; set; }
    public bool NeedsReview { get; set; }
}
