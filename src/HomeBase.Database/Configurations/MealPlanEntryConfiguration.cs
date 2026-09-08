using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable(
            "meal_plan_entry",
            t =>
                t.HasCheckConstraint(
                    "ck_meal_plan_entry_recipe_or_text",
                    "recipe_id IS NOT NULL OR free_text IS NOT NULL"
                )
        );

        builder.Property(x => x.Slot).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.FreeText).HasMaxLength(200);

        builder.Property(x => x.Servings).HasDefaultValue(2);

        builder.HasIndex(x => x.PlanDate);

        builder
            .HasOne(x => x.Recipe)
            .WithMany()
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
