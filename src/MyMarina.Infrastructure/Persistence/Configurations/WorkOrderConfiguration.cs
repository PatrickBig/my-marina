using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Title).HasMaxLength(200).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(4000).IsRequired();
        builder.Property(w => w.Notes).HasMaxLength(4000);
        builder.Property(w => w.Status).HasConversion<string>();
        builder.Property(w => w.Priority).HasConversion<string>();

        builder.HasOne(w => w.Marina)
               .WithMany()
               .HasForeignKey(w => w.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.MarinaId);
        builder.HasIndex(w => new { w.MarinaId, w.Status });
    }
}
