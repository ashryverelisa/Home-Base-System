using HomeBase.Database.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class PromoSavingConfiguration : IEntityTypeConfiguration<PromoSaving>
{
    public void Configure(EntityTypeBuilder<PromoSaving> builder)
    {
        builder.HasNoKey().ToView("v_promo_savings");

        builder.Property(x => x.DiscountTotal).HasPrecision(12, 2);
        builder.Property(x => x.SavedAgainstNormal).HasPrecision(12, 2);
    }
}
