using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MarinaVesselRecordConfiguration : IEntityTypeConfiguration<MarinaVesselRecord>
{
    public void Configure(EntityTypeBuilder<MarinaVesselRecord> builder)
    {
        builder.ToTable("marina_vessel_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.InsuranceProvider).HasMaxLength(200);
        builder.Property(r => r.InsurancePolicyNumber).HasMaxLength(100);

        builder.HasOne(r => r.Marina)
               .WithMany()
               .HasForeignKey(r => r.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Vessel)
               .WithMany()
               .HasForeignKey(r => r.VesselId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.BillingAccount)
               .WithMany(b => b.VesselRecords)
               .HasForeignKey(r => r.BillingAccountId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(r => r.Marina.IsSetupComplete);

        // One record per (marina, vessel) pair
        builder.HasIndex(r => new { r.MarinaId, r.VesselId }).IsUnique();
        builder.HasIndex(r => r.BillingAccountId)
               .HasFilter("\"BillingAccountId\" IS NOT NULL");
    }
}
