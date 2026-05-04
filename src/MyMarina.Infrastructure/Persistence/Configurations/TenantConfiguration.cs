using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        builder.Property(t => t.BillingEmail).HasMaxLength(256);
        builder.Property(t => t.SubscriptionTier).HasConversion<string>();

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}
