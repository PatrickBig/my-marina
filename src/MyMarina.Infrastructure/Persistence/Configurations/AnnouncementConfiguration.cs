using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Body).HasMaxLength(10000).IsRequired();
        builder.Property(a => a.Audience).HasConversion<string>();

        builder.HasOne(a => a.Marina)
               .WithMany()
               .HasForeignKey(a => a.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(a => a.Marina.IsSetupComplete);

        builder.HasIndex(a => a.MarinaId);
        builder.HasIndex(a => new { a.MarinaId, a.PublishedAt });
    }
}
