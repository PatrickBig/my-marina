using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class VesselConfiguration : IEntityTypeConfiguration<Vessel>
{
    public void Configure(EntityTypeBuilder<Vessel> builder)
    {
        builder.ToTable("vessels");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Make).HasMaxLength(100);
        builder.Property(v => v.Model).HasMaxLength(100);
        builder.Property(v => v.HullColor).HasMaxLength(50);
        builder.Property(v => v.RegistrationNumber).HasMaxLength(50);
        builder.Property(v => v.RegistrationState).HasMaxLength(50);
        builder.Property(v => v.ClaimEmail).HasMaxLength(256);

        builder.Property(v => v.Length).HasPrecision(8, 2);
        builder.Property(v => v.Beam).HasPrecision(8, 2);
        builder.Property(v => v.Draft).HasPrecision(8, 2);

        builder.Property(v => v.BoatType).HasConversion<string>();

        builder.HasIndex(v => v.OwnerUserId);
        builder.HasIndex(v => v.ClaimEmail)
               .HasFilter("\"ClaimEmail\" IS NOT NULL");
    }
}
