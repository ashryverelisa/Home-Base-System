namespace HomeBase.Database.Enums;

public enum StockMovementType
{
    Purchase,
    Consume,
    Waste,
    Correction,
    Move,
}

public enum AssetStatus
{
    InUse,
    Stored,
    Broken,
    Sold,
    Disposed,
}

public enum AssetDocumentType
{
    Invoice,
    Manual,
    Photo,
    Warranty,
}
