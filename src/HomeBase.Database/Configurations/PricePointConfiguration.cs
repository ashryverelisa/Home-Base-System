using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class PricePointConfiguration : IEntityTypeConfiguration<PricePoint>
{
    public void Configure(EntityTypeBuilder<PricePoint> builder)
    {
        builder.HasNoKey().ToView("v_price_history");

        builder.Property(x => x.PricePerBaseUnit).HasPrecision(14, 6);
        builder.Property(x => x.PrevPrice).HasPrecision(14, 6);
    }
}
