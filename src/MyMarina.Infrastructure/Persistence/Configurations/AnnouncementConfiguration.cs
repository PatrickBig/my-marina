using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Body).HasMaxLength(20000).IsRequired();
        builder.HasOne(e => e.Marina)
            .WithMany()
            .HasForeignKey(e => e.MarinaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.TenantId, e.MarinaId, e.PublishedAt });
    }
}
