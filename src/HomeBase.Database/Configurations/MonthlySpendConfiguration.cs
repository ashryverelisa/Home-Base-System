using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class MonthlySpendConfiguration : IEntityTypeConfiguration<MonthlySpend>
{
    public void Configure(EntityTypeBuilder<MonthlySpend> builder)
    {
        builder.HasNoKey().ToView("v_monthly_spend");

        builder.Property(x => x.Total).HasPrecision(12, 2);
    }
}
