using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");

        builder.Property(x => x.BaseUnit).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Brand).HasMaxLength(100);
        builder.Property(x => x.Gtin).HasMaxLength(14);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.OffId).HasMaxLength(50);
        builder.Property(x => x.Notes).HasColumnType("text");

        builder.Property(x => x.PackageSize).HasPrecision(12, 3).HasDefaultValue(1m);
        builder.Property(x => x.PieceWeightBase).HasPrecision(12, 3);
        builder.Property(x => x.MinStockBase).HasPrecision(12, 3);
        builder.Property(x => x.TargetStockBase).HasPrecision(12, 3);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(x => x.Gtin).IsUnique();

        builder.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder
            .HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.DefaultLocation)
            .WithMany()
            .HasForeignKey(x => x.DefaultLocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
