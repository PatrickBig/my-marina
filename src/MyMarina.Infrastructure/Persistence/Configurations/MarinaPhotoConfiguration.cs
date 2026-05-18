using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MarinaPhotoConfiguration : IEntityTypeConfiguration<MarinaPhoto>
{
    public void Configure(EntityTypeBuilder<MarinaPhoto> builder)
    {
        builder.ToTable("marina_photos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Kind).HasConversion<int>();
        builder.Property(p => p.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(p => p.UrlFull).HasMaxLength(1000);
        builder.Property(p => p.UrlMedium).HasMaxLength(1000);
        builder.Property(p => p.UrlThumbnail).HasMaxLength(1000);
        builder.Property(p => p.Caption).HasMaxLength(500);
        builder.Property(p => p.Latitude).HasPrecision(9, 6);
        builder.Property(p => p.Longitude).HasPrecision(9, 6);

        builder.HasOne(p => p.Marina)
               .WithMany(m => m.Photos)
               .HasForeignKey(p => p.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        // Unique: at most one Logo and one Banner per marina
        builder.HasIndex(p => new { p.MarinaId, p.Kind })
               .IsUnique()
               .HasFilter($"\"Kind\" IN ({(int)MarinaPhotoKind.Logo}, {(int)MarinaPhotoKind.Banner})")
               .HasDatabaseName("ix_marina_photos_marina_logo_banner_unique");

        builder.HasIndex(p => p.MarinaId);
        builder.HasIndex(p => new { p.MarinaId, p.Kind, p.SortOrder });
    }
}
