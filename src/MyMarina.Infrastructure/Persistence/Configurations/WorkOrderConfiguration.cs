using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.HasOne(e => e.MaintenanceRequest)
            .WithOne(mr => mr.WorkOrder)
            .HasForeignKey<WorkOrder>(e => e.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
