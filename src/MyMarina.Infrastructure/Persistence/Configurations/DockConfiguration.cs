using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class DockConfiguration : IEntityTypeConfiguration<Dock>
{
    public void Configure(EntityTypeBuilder<Dock> builder)
    {
        builder.ToTable("docks");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);

        builder.HasIndex(d => d.MarinaId);
    }
}
