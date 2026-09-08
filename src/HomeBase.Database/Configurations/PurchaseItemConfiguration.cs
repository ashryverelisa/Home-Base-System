using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_item");

        builder.Property(x => x.LineType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.MatchStatus).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.RawText).HasMaxLength(300);
        builder.Property(x => x.Gtin).HasMaxLength(14);

        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.Property(x => x.QuantityBase).HasPrecision(12, 3);
        builder.Property(x => x.UnitPrice).HasPrecision(14, 6);
        builder.Property(x => x.LineTotal).HasPrecision(12, 2);
        builder.Property(x => x.Discount).HasPrecision(12, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);

        builder
            .Property(x => x.PricePerBaseUnit)
            .HasPrecision(14, 6)
            .HasComputedColumnSql("line_total / NULLIF(quantity_base, 0)", stored: true);

        builder.HasIndex(x => new { x.PurchaseId, x.LineNo }).IsUnique();
        builder.HasIndex(x => x.ProductId);

        builder
            .HasOne(x => x.Purchase)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.ParentItem)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.SuggestedProduct)
            .WithMany()
            .HasForeignKey(x => x.SuggestedProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
