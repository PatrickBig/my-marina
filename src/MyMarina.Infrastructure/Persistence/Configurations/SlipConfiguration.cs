using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class SlipConfiguration : IEntityTypeConfiguration<Slip>
{
    public void Configure(EntityTypeBuilder<Slip> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.MaxLength).HasPrecision(8, 2);
        builder.Property(e => e.MaxBeam).HasPrecision(8, 2);
        builder.Property(e => e.MaxDraft).HasPrecision(8, 2);
        builder.Property(e => e.DailyRate).HasPrecision(10, 2);
        builder.Property(e => e.MonthlyRate).HasPrecision(10, 2);
        builder.Property(e => e.AnnualRate).HasPrecision(10, 2);
        builder.Property(e => e.Latitude).HasPrecision(10, 7);
        builder.Property(e => e.Longitude).HasPrecision(11, 7);

        // DockId is nullable — null = free-standing mooring or anchorage
        builder.HasOne(e => e.Dock).WithMany(d => d.Slips)
            .HasForeignKey(e => e.DockId)
            .IsRequired(false);
    }
}
