using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MarinaConfiguration : IEntityTypeConfiguration<Marina>
{
    public void Configure(EntityTypeBuilder<Marina> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Website).HasMaxLength(500);
        builder.OwnsOne(e => e.Address, addr =>
        {
            addr.Property(a => a.Street).HasMaxLength(200).HasColumnName("AddressStreet");
            addr.Property(a => a.City).HasMaxLength(100).HasColumnName("AddressCity");
            addr.Property(a => a.State).HasMaxLength(100).HasColumnName("AddressState");
            addr.Property(a => a.Zip).HasMaxLength(20).HasColumnName("AddressZip");
            addr.Property(a => a.Country).HasMaxLength(100).HasColumnName("AddressCountry");
        });
        builder.HasMany(e => e.Docks).WithOne(d => d.Marina).HasForeignKey(d => d.MarinaId);
        builder.HasMany(e => e.Slips).WithOne(s => s.Marina).HasForeignKey(s => s.MarinaId);
        builder.HasMany(e => e.Announcements).WithOne(a => a.Marina).HasForeignKey(a => a.MarinaId);
    }
}
