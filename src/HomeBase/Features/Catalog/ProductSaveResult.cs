namespace HomeBase.Features.Catalog;

public sealed record ProductSaveResult(bool Succeeded, int ProductId, string? Error)
{
    public static ProductSaveResult Ok(int productId) => new(true, productId, null);

    public static ProductSaveResult Failed(string error) => new(false, 0, error);
}