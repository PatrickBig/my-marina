using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount>
{
    public void Configure(EntityTypeBuilder<CustomerAccount> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.BillingEmail).HasMaxLength(200).IsRequired();
        builder.Property(e => e.BillingPhone).HasMaxLength(50);
        builder.OwnsOne(e => e.BillingAddress, addr =>
        {
            addr.Property(a => a.Street).HasMaxLength(200).HasColumnName("BillingStreet");
            addr.Property(a => a.City).HasMaxLength(100).HasColumnName("BillingCity");
            addr.Property(a => a.State).HasMaxLength(100).HasColumnName("BillingState");
            addr.Property(a => a.Zip).HasMaxLength(20).HasColumnName("BillingZip");
            addr.Property(a => a.Country).HasMaxLength(100).HasColumnName("BillingCountry");
        });
        builder.HasMany(e => e.Members).WithOne(m => m.CustomerAccount)
            .HasForeignKey(m => m.CustomerAccountId);
        builder.HasMany(e => e.Boats).WithOne(b => b.CustomerAccount)
            .HasForeignKey(b => b.CustomerAccountId);
        builder.HasMany(e => e.Invoices).WithOne(i => i.CustomerAccount)
            .HasForeignKey(i => i.CustomerAccountId);
        builder.HasMany(e => e.SlipAssignments).WithOne(sa => sa.CustomerAccount)
            .HasForeignKey(sa => sa.CustomerAccountId);
        builder.HasMany(e => e.MaintenanceRequests).WithOne(mr => mr.CustomerAccount)
            .HasForeignKey(mr => mr.CustomerAccountId);
    }
}
