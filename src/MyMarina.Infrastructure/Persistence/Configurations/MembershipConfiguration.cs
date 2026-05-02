using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Scope).HasConversion<string>();
        builder.Property(m => m.Role).HasConversion<string>();

        builder.HasOne(m => m.Tenant)
               .WithMany()
               .HasForeignKey(m => m.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.UserId, m.MarinaId })
               .HasFilter("marina_id IS NOT NULL");
        builder.HasIndex(m => new { m.UserId, m.TenantId });
    }
}
