using HomeBase.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeBase.Database.Configurations;

public class AssetDocumentConfiguration : IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetDocument> builder)
    {
        builder.ToTable("asset_document");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);

        builder.Property(x => x.FilePath).HasMaxLength(512);

        builder.Property(x => x.UploadedAt).HasDefaultValueSql("now()");

        builder.HasOne(x => x.Asset)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.AssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
