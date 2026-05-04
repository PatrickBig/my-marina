using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Status).IsRequired();
        builder.Property(i => i.SubTotal).HasColumnType("numeric(18,2)");
        builder.Property(i => i.TaxAmount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(i => i.AmountPaid).HasColumnType("numeric(18,2)");
        builder.Property(i => i.Notes).HasMaxLength(2000);

        // BalanceDue is computed in code; ignore for persistence
        builder.Ignore(i => i.BalanceDue);

        builder.HasOne(i => i.Marina)
               .WithMany()
               .HasForeignKey(i => i.MarinaId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.BillingAccount)
               .WithMany()
               .HasForeignKey(i => i.BillingAccountId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.LineItems)
               .WithOne(l => l.Invoice)
               .HasForeignKey(l => l.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
               .WithOne(p => p.Invoice)
               .HasForeignKey(p => p.InvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.MarinaId);
        builder.HasIndex(i => i.BillingAccountId);
        builder.HasIndex(i => new { i.MarinaId, i.InvoiceNumber }).IsUnique();
        builder.HasIndex(i => new { i.MarinaId, i.Status });
    }
}
