using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "mymarina");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.TargetType).HasMaxLength(50);
        builder.Property(a => a.TargetId).HasMaxLength(100);
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.ActorUserId);
    }
}
