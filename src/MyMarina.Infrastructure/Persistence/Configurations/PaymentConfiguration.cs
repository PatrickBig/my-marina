using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("numeric(18,2)");
        builder.Property(p => p.Method).IsRequired();
        builder.Property(p => p.ReferenceNumber).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.PaymentProviderId).HasMaxLength(200);
        builder.Property(p => p.PaymentProviderReference).HasMaxLength(200);

        builder.HasIndex(p => p.InvoiceId);
    }
}
