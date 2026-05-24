using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class SlipAssignmentConfiguration : IEntityTypeConfiguration<SlipAssignment>
{
    public void Configure(EntityTypeBuilder<SlipAssignment> builder)
    {
        builder.ToTable("slip_assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssignmentType).HasConversion<string>();
        builder.Property(a => a.BaseRate).HasPrecision(10, 2);
        builder.Property(a => a.OwnerSubletShareToHolder).HasPrecision(5, 4);
        builder.Property(a => a.HolderSubletShareToOwner).HasPrecision(5, 4);

        builder.HasOne(a => a.Slip)
               .WithMany()
               .HasForeignKey(a => a.SlipId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.BillingAccount)
               .WithMany()
               .HasForeignKey(a => a.BillingAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => a.BillingAccount.Marina.IsSetupComplete);

        builder.HasOne(a => a.Vessel)
               .WithMany()
               .HasForeignKey(a => a.VesselId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.SlipId);
        builder.HasIndex(a => a.BillingAccountId);
        builder.HasIndex(a => new { a.SlipId, a.StartDate });
    }
}
