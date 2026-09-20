using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class DepositBalanceConfiguration : IEntityTypeConfiguration<DepositBalance>
{
    public void Configure(EntityTypeBuilder<DepositBalance> builder)
    {
        builder.HasNoKey().ToView("v_deposit_balance");

        builder.Property(x => x.OpenDeposit).HasPrecision(12, 2);
    }
}
