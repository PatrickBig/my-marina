using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(4000).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>();
        builder.Property(r => r.Priority).HasConversion<string>();

        builder.HasOne(r => r.Marina)
               .WithMany()
               .HasForeignKey(r => r.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.WorkOrder)
               .WithOne(w => w.MaintenanceRequest)
               .HasForeignKey<WorkOrder>(w => w.MaintenanceRequestId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.MarinaId);
        builder.HasIndex(r => r.BoaterUserId);
        builder.HasIndex(r => new { r.MarinaId, r.Status });
    }
}
