using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.ToTable("stock_lot");

        builder.Property(x => x.QuantityBase).HasPrecision(12, 3);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => x.ProductId).HasFilter("quantity_base > 0");
        builder.HasIndex(x => x.BestBefore).HasFilter("quantity_base > 0");

        builder
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(x => x.PurchaseItem)
            .WithMany()
            .HasForeignKey(x => x.PurchaseItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
