using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class OperatingExpenseConfiguration : IEntityTypeConfiguration<OperatingExpense>
{
    public void Configure(EntityTypeBuilder<OperatingExpense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Category).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(12, 2);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(100);
        builder.HasOne<Marina>()
            .WithMany()
            .HasForeignKey(e => e.MarinaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.TenantId, e.MarinaId, e.IncurredDate });
    }
}
