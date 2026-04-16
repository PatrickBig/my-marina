using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000).IsRequired();
        builder.HasOne(e => e.CustomerAccount)
            .WithMany(c => c.MaintenanceRequests)
            .HasForeignKey(e => e.CustomerAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
