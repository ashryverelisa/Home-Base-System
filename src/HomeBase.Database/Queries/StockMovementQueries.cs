using HomeBase.Database.Entities;

namespace HomeBase.Database.Queries;

public static class StockMovementQueries
{
    public static IQueryable<StockMovement> ForProduct(
        this IQueryable<StockMovement> movements,
        int productId) =>
        movements.Where(m => m.ProductId == productId);

    public static IQueryable<StockMovement> ForLot(
        this IQueryable<StockMovement> movements,
        long lotId) =>
        movements.Where(m => m.LotId == lotId);

    public static IQueryable<StockMovement> Newest(
        this IQueryable<StockMovement> movements,
        int take) =>
        movements.OrderByDescending(m => m.OccurredAt).ThenByDescending(m => m.Id).Take(take);
}
