using HomeBase.Database.Enums;

namespace HomeBase.Database.Entities;

public class StorageLocation
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public StorageLocation? Parent { get; set; }
    public List<StorageLocation> Children { get; set; } = [];
    public required string Name { get; set; }
    public StorageZone Zone { get; set; } = StorageZone.Ambient;
}
