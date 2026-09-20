using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class EffectiveLineConfiguration : IEntityTypeConfiguration<EffectiveLine>
{
    public void Configure(EntityTypeBuilder<EffectiveLine> builder)
    {
        builder.HasNoKey().ToView("v_effective_line");

        builder.Property(x => x.QuantityBase).HasPrecision(12, 3);
        builder.Property(x => x.EffectiveTotal).HasPrecision(12, 2);
        builder.Property(x => x.EffectivePricePerBaseUnit).HasPrecision(14, 6);
    }
}
