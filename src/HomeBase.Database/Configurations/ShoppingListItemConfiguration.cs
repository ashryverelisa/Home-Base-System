using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.ToTable(
            "shopping_list_item",
            t =>
                t.HasCheckConstraint(
                    "ck_shopping_list_item_product_or_text",
                    "product_id IS NOT NULL OR free_text IS NOT NULL"
                )
        );

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.AddedBy).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.FreeText).HasMaxLength(200);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.Property(x => x.TargetPrice).HasPrecision(12, 2);
        builder.Property(x => x.AddedAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => new
        {
            x.ListId,
            x.Status,
            x.AddedBy,
        });

        builder
            .HasOne(x => x.List)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.PurchaseItem)
            .WithMany()
            .HasForeignKey(x => x.PurchaseItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
