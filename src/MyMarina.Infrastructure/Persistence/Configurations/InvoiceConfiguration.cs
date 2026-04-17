using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.InvoiceNumber }).IsUnique();
        builder.Property(e => e.SubTotal).HasPrecision(12, 2);
        builder.Property(e => e.TaxAmount).HasPrecision(12, 2);
        builder.Property(e => e.TotalAmount).HasPrecision(12, 2);
        builder.Property(e => e.AmountPaid).HasPrecision(12, 2);

        // BalanceDue is a computed property — not persisted
        builder.Ignore(e => e.BalanceDue);

        builder.HasOne<Marina>()
            .WithMany()
            .HasForeignKey(e => e.MarinaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.LineItems).WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId);
        builder.HasMany(e => e.Payments).WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId);
    }
}
