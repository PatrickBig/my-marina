using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class SlipLeaseInquiryConfiguration : IEntityTypeConfiguration<SlipLeaseInquiry>
{
    public void Configure(EntityTypeBuilder<SlipLeaseInquiry> builder)
    {
        builder.ToTable("slip_lease_inquiries");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DesiredTerm).HasConversion<string>();
        builder.Property(i => i.Status).HasConversion<string>();
        builder.Property(i => i.AgreedRateKind).HasConversion<string?>();

        builder.Property(i => i.AgreedBaseRate).HasPrecision(10, 2);
        builder.Property(i => i.Message).HasMaxLength(2000);
        builder.Property(i => i.MarinaNote).HasMaxLength(2000);

        builder.HasOne(i => i.Slip)
               .WithMany()
               .HasForeignKey(i => i.SlipId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.MarinaId);
        builder.HasIndex(i => i.RequestingUserId);
        builder.HasIndex(i => new { i.SlipId, i.Status });
    }
}
