using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class BillingAccountMemberConfiguration : IEntityTypeConfiguration<BillingAccountMember>
{
    public void Configure(EntityTypeBuilder<BillingAccountMember> builder)
    {
        builder.ToTable("billing_account_members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).HasConversion<string>();

        builder.HasOne(m => m.BillingAccount)
               .WithMany(b => b.Members)
               .HasForeignKey(m => m.BillingAccountId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(m => m.BillingAccount.Marina.IsSetupComplete);

        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.BillingAccountId, m.UserId }).IsUnique();
    }
}
