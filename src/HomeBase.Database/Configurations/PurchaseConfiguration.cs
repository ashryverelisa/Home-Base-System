using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchase");

        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.PaymentMethod).HasMaxLength(50);
        builder.Property(x => x.ExternalId).HasMaxLength(128);
        builder.Property(x => x.ReceiptFile).HasMaxLength(512);

        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().HasDefaultValue("EUR");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.Property(x => x.RawPayload).HasColumnType("jsonb");
        builder.HasIndex(x => x.ExternalId).IsUnique();

        builder.HasIndex(x => x.PurchasedAt);

        builder
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
