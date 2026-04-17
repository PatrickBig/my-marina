using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class UserContextConfiguration : IEntityTypeConfiguration<UserContext>
{
    public void Configure(EntityTypeBuilder<UserContext> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Role)
            .WithMany(r => r.UserContexts)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.UserId, e.TenantId, e.MarinaId });
    }
}
