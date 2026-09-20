using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class BestStorePriceConfiguration : IEntityTypeConfiguration<BestStorePrice>
{
    public void Configure(EntityTypeBuilder<BestStorePrice> builder)
    {
        builder.HasNoKey().ToView("v_best_store");

        builder.Property(x => x.AveragePrice).HasPrecision(14, 6);
        builder.Property(x => x.BestPrice).HasPrecision(14, 6);
    }
}
