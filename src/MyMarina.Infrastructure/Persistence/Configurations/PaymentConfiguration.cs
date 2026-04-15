using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(200);
        builder.Property(e => e.PaymentProviderId).HasMaxLength(200);
        builder.Property(e => e.PaymentProviderReference).HasMaxLength(500);
    }
}
