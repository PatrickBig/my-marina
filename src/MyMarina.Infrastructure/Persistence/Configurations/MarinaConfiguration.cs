using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class MarinaConfiguration : IEntityTypeConfiguration<Marina>
{
    public void Configure(EntityTypeBuilder<Marina> builder)
    {
        builder.ToTable("marinas");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Slug).HasMaxLength(100).IsRequired();
        builder.Property(m => m.AddressStreet).HasMaxLength(200);
        builder.Property(m => m.AddressCity).HasMaxLength(100);
        builder.Property(m => m.AddressState).HasMaxLength(100);
        builder.Property(m => m.AddressZip).HasMaxLength(20);
        builder.Property(m => m.AddressCountry).HasMaxLength(100);
        builder.Property(m => m.PhoneNumber).HasMaxLength(50);
        builder.Property(m => m.Email).HasMaxLength(256);
        builder.Property(m => m.Website).HasMaxLength(500);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(m => m.MarinaType).HasConversion<string>();

        builder.Property(m => m.Latitude).HasPrecision(9, 6);
        builder.Property(m => m.Longitude).HasPrecision(9, 6);

        builder.HasOne(m => m.Tenant)
               .WithMany()
               .HasForeignKey(m => m.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.IsSetupComplete).HasDefaultValue(false).IsRequired();
        builder.Property(m => m.SetupStep).HasDefaultValue(0).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasIndex(m => m.Slug).IsUnique();
        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.IsSetupComplete);
    }
}
