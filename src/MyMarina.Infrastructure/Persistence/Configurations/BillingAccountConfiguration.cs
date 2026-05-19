using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class BillingAccountConfiguration : IEntityTypeConfiguration<BillingAccount>
{
    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        builder.ToTable("billing_accounts");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(b => b.BillingEmail).HasMaxLength(256).IsRequired();
        builder.Property(b => b.BillingPhone).HasMaxLength(50);
        builder.Property(b => b.BillingAddressStreet).HasMaxLength(200);
        builder.Property(b => b.BillingAddressCity).HasMaxLength(100);
        builder.Property(b => b.BillingAddressState).HasMaxLength(100);
        builder.Property(b => b.BillingAddressZip).HasMaxLength(20);
        builder.Property(b => b.BillingAddressCountry).HasMaxLength(100);
        builder.Property(b => b.EmergencyContactName).HasMaxLength(200);
        builder.Property(b => b.EmergencyContactPhone).HasMaxLength(50);

        builder.HasOne(b => b.Marina)
               .WithMany()
               .HasForeignKey(b => b.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(b => b.Marina.IsSetupComplete);

        builder.HasIndex(b => b.MarinaId);
        builder.HasIndex(b => new { b.MarinaId, b.BillingEmail });
    }
}
