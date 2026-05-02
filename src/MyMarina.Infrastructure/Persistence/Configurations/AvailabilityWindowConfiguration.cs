using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class AvailabilityWindowConfiguration : IEntityTypeConfiguration<AvailabilityWindow>
{
    public void Configure(EntityTypeBuilder<AvailabilityWindow> builder)
    {
        builder.ToTable("availability_windows");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.ListedByKind).HasConversion<string>();
        builder.Property(w => w.Status).HasConversion<string>();

        builder.Property(w => w.BasePricePerNight).HasPrecision(10, 2);
        builder.Property(w => w.WeeklyDiscount).HasPrecision(5, 4);
        builder.Property(w => w.MonthlyDiscount).HasPrecision(5, 4);
        builder.Property(w => w.CleaningFee).HasPrecision(10, 2);

        // RevenueSplit stored as a JSONB array
        builder.OwnsMany(w => w.RevenueSplit, split =>
        {
            split.ToJson("revenue_split");
            split.Property(s => s.PayeeKind).HasMaxLength(50);
            split.Property(s => s.Percent).HasPrecision(5, 4);
        });

        builder.HasOne(w => w.Slip)
               .WithMany()
               .HasForeignKey(w => w.SlipId)
               .OnDelete(DeleteBehavior.Cascade);

        // Fast lookup by slip + date range
        builder.HasIndex(w => new { w.SlipId, w.StartsAt });
        builder.HasIndex(w => w.Status)
               .HasFilter("status IN ('Open', 'Paused')");
        builder.HasIndex(w => w.ListedByMarinaId)
               .HasFilter("listed_by_marina_id IS NOT NULL");
    }
}
