using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movement");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.Reason).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.Property(x => x.QuantityDelta).HasPrecision(12, 3);
        builder.Property(x => x.OccurredAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => new { x.ProductId, x.OccurredAt });

        builder
            .HasOne(x => x.Lot)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.LotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.MealPlanEntry)
            .WithMany(x => x.Movements)
            .HasForeignKey(x => x.MealPlanEntryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
