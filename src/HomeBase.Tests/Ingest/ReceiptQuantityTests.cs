using HomeBase.Database.Enums;
using HomeBase.Features.Ingest;

namespace HomeBase.Tests.Ingest;

public class ReceiptQuantityTests
{
    [Theory]
    [InlineData(0.75, "kg", BaseUnit.Gram, 500, 750)]
    [InlineData(330, "ml", BaseUnit.Milliliter, 1000, 330)]
    [InlineData(2, null, BaseUnit.Gram, 500, 1000)]
    [InlineData(3, "stk", BaseUnit.Piece, 6, 18)]
    public void ToBaseQuantity_WeighedOrCountedInPackages(
        double quantity,
        string? unit,
        BaseUnit baseUnit,
        double packageSize,
        double expected
    )
    {
        Assert.Equal(
            (decimal)expected,
            ReceiptIngestService.ToBaseQuantity(
                (decimal)quantity,
                unit,
                baseUnit,
                (decimal)packageSize
            )
        );
    }
}
