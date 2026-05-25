using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class PricingPlanConfiguration : IEntityTypeConfiguration<PricingPlan>
{
    public void Configure(EntityTypeBuilder<PricingPlan> builder)
    {
        builder.ToTable("pricing_plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.TransientRateKind).HasConversion<string?>();
        builder.Property(p => p.LeaseRateKind).HasConversion<string?>();
        builder.Property(p => p.TransientAmount).HasPrecision(10, 2);
        builder.Property(p => p.TransientMinCharge).HasPrecision(10, 2);
        builder.Property(p => p.LeaseAmount).HasPrecision(10, 2);
        builder.Property(p => p.LeaseMinCharge).HasPrecision(10, 2);

        builder.OwnsMany(p => p.AmenityAddOns, ao =>
        {
            ao.ToJson("add_ons");
            ao.Property(a => a.Amenity).HasConversion<string>();
            ao.Property(a => a.TransientAmount).HasPrecision(10, 2);
            ao.Property(a => a.LeaseAmount).HasPrecision(10, 2);
        });

        builder.HasOne(p => p.Marina)
               .WithMany()
               .HasForeignKey(p => p.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        // Unique plan name per marina
        builder.HasIndex(p => new { p.MarinaId, p.Name }).IsUnique();

        // Filtered index: only one default per marina (enforced at DB level)
        builder.HasIndex(p => p.MarinaId)
               .HasFilter("\"IsDefault\" = true")
               .IsUnique()
               .HasDatabaseName("ix_pricing_plans_marina_single_default");

        builder.HasQueryFilter(p => p.Marina.IsSetupComplete);

        builder.HasIndex(p => p.MarinaId);
    }
}
