namespace HomeBase.Database.Entities;

// Locations and categories form the same two-level tree and share their editing rules.
public interface ITreeNode
{
    int Id { get; }
    int? ParentId { get; set; }
    string Name { get; set; }
}
