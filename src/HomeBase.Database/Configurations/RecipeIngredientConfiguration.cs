using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable(
            "recipe_ingredient",
            t =>
                t.HasCheckConstraint(
                    "ck_recipe_ingredient_product_or_text",
                    "product_id IS NOT NULL OR free_text IS NOT NULL"
                )
        );

        builder.Property(x => x.FreeText).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(200);

        builder.Property(x => x.QuantityBase).HasPrecision(12, 3);

        builder
            .HasOne(x => x.Recipe)
            .WithMany(x => x.Ingredients)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
