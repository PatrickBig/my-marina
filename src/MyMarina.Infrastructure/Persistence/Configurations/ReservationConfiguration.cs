using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Status).HasConversion<string>();
        builder.Property(r => r.PaymentStatus).HasConversion<string>();
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.CancellationPolicySnapshot).HasColumnType("jsonb");

        builder.Property(r => r.BasePrice).HasPrecision(10, 2);
        builder.Property(r => r.Fees).HasPrecision(10, 2);
        builder.Property(r => r.Taxes).HasPrecision(10, 2);
        builder.Property(r => r.Total).HasPrecision(10, 2);
        builder.Property(r => r.PlatformFeeAmount).HasPrecision(10, 2);

        // Frozen revenue-split snapshot stored as JSONB array
        builder.OwnsMany(r => r.RevenueSplitSnapshot, split =>
        {
            split.ToJson("revenue_split_snapshot");
            split.Property(s => s.PayeeKind).HasMaxLength(50);
            split.Property(s => s.Percent).HasPrecision(5, 4);
        });

        builder.HasOne(r => r.AvailabilityWindow)
               .WithMany()
               .HasForeignKey(r => r.AvailabilityWindowId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Vessel)
               .WithMany()
               .HasForeignKey(r => r.VesselId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Slip)
               .WithMany()
               .HasForeignKey(r => r.SlipId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.BoaterUserId);
        builder.HasIndex(r => new { r.SlipId, r.ArrivesAt });
        builder.HasIndex(r => r.AvailabilityWindowId);
        builder.HasIndex(r => r.Status)
               .HasFilter("status IN ('PendingApproval','PendingHostMarinaApproval','Confirmed')");
    }
}
