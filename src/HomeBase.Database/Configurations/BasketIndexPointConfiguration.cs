using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class BasketIndexPointConfiguration : IEntityTypeConfiguration<BasketIndexPoint>
{
    public void Configure(EntityTypeBuilder<BasketIndexPoint> builder)
    {
        builder.HasNoKey().ToView("v_basket_index");

        builder.Property(x => x.IndexValue).HasPrecision(12, 2);
    }
}
