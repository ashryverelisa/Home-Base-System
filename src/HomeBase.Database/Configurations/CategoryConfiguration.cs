using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("category");

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Icon).HasMaxLength(64);

        builder.HasIndex(x => new { x.ParentId, x.Name }).IsUnique();

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
