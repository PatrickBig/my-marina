using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Persistence.Configurations;

public class OwnerAbsenceConfiguration : IEntityTypeConfiguration<OwnerAbsence>
{
    public void Configure(EntityTypeBuilder<OwnerAbsence> builder)
    {
        builder.ToTable("owner_absences");
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Assignment)
               .WithMany()
               .HasForeignKey(a => a.SlipAssignmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.SlipAssignmentId);
        builder.HasIndex(a => a.SlipId);
        builder.HasIndex(a => new { a.SlipId, a.StartsOn, a.EndsOn });
    }
}
