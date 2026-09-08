using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class ProductAliasConfiguration : IEntityTypeConfiguration<ProductAlias>
{
    public void Configure(EntityTypeBuilder<ProductAlias> builder)
    {
        builder.ToTable("product_alias");

        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.RawText).HasMaxLength(200);
        builder.Property(x => x.NormalizedText).HasMaxLength(200);

        builder.Property(x => x.TimesSeen).HasDefaultValue(1);

        builder.HasIndex(x => new { x.StoreId, x.NormalizedText }).IsUnique();

        builder.HasIndex(x => x.NormalizedText).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder
            .HasOne(x => x.Product)
            .WithMany(x => x.Aliases)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
