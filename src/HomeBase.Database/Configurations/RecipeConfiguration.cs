using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipe");

        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.SourceUrl).HasMaxLength(1000);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Instructions).HasColumnType("text");

        builder.Property(x => x.Servings).HasDefaultValue(2);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder.Property(x => x.Tags)
            .HasColumnType("text[]")
            .HasDefaultValueSql("'{}'::text[]");

        builder.HasIndex(x => x.Name);
    }
}
