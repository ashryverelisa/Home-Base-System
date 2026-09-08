namespace HomeBase.Database.Entities;

public class Store
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Chain { get; set; }
    public string? Address { get; set; }
    public bool IsOnline { get; set; }
    public string? TaxId { get; set; }
}
