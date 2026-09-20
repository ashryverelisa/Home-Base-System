using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class WasteCostConfiguration : IEntityTypeConfiguration<WasteCost>
{
    public void Configure(EntityTypeBuilder<WasteCost> builder)
    {
        builder.HasNoKey().ToView("v_waste_cost");

        builder.Property(x => x.QuantityBase).HasPrecision(12, 3);
        builder.Property(x => x.Cost).HasPrecision(12, 2);
    }
}
