using System.Text.Json;
using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeBase.Database.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("asset");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasColumnType("text");
        builder.Property(x => x.PurchasePrice).HasPrecision(12, 2);
        builder.Property(x => x.CurrentValue).HasPrecision(12, 2);

        var converter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v =>
                JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions)
                ?? new Dictionary<string, string>()
        );

        var comparer = new ValueComparer<Dictionary<string, string>>(
            (a, b) =>
                JsonSerializer.Serialize(a, JsonOptions)
                == JsonSerializer.Serialize(b, JsonOptions),
            v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
            v =>
                JsonSerializer.Deserialize<Dictionary<string, string>>(
                    JsonSerializer.Serialize(v, JsonOptions),
                    JsonOptions
                ) ?? new Dictionary<string, string>()
        );

        builder
            .Property(x => x.Attributes)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(converter, comparer);

        builder.HasIndex(x => x.SerialNumber);

        builder
            .HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

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
