using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class ConsumptionRateConfiguration : IEntityTypeConfiguration<ConsumptionRate>
{
    public void Configure(EntityTypeBuilder<ConsumptionRate> builder)
    {
        builder.HasNoKey().ToView("v_consumption_rate");

        builder.Property(x => x.PerWeek).HasPrecision(12, 3);
    }
}
