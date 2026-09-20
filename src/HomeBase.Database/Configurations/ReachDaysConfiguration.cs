using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class ReachDaysConfiguration : IEntityTypeConfiguration<ReachDays>
{
    public void Configure(EntityTypeBuilder<ReachDays> builder)
    {
        builder.HasNoKey().ToView("v_reach_days");

        builder.Property(x => x.StockBase).HasPrecision(12, 3);
        builder.Property(x => x.PerWeek).HasPrecision(12, 3);
        builder.Property(x => x.DaysLeft).HasPrecision(12, 2);
    }
}
