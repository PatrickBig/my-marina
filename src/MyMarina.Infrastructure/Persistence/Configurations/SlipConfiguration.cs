using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class SlipConfiguration : IEntityTypeConfiguration<Slip>
{
    public void Configure(EntityTypeBuilder<Slip> builder)
    {
        builder.ToTable("slips");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.SlipType).HasConversion<string>();
        builder.Property(s => s.Status).HasConversion<string>();
        builder.Property(s => s.HostMarinaPolicy).HasConversion<string>();
        builder.Property(s => s.Electric).HasConversion<int?>();

        builder.Property(s => s.MaxLength).HasPrecision(8, 2);
        builder.Property(s => s.MaxBeam).HasPrecision(8, 2);
        builder.Property(s => s.MaxDraft).HasPrecision(8, 2);
        builder.Property(s => s.Latitude).HasPrecision(9, 6);
        builder.Property(s => s.Longitude).HasPrecision(9, 6);

        builder.Property(s => s.AddressStreet).HasMaxLength(200);
        builder.Property(s => s.AddressCity).HasMaxLength(100);
        builder.Property(s => s.AddressState).HasMaxLength(100);
        builder.Property(s => s.AddressZip).HasMaxLength(20);
        builder.Property(s => s.AddressCountry).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.HasIndex(s => s.MarinaId);
        builder.HasIndex(s => s.DockId).HasFilter("dock_id IS NOT NULL");
    }
}
