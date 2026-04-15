using MyMarina.Application.Abstractions;
using MyMarina.Application.Slips;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Slips;

public class CreateSlipCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateSlipCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateSlipCommand command, CancellationToken ct = default)
    {
        var slip = new Slip
        {
            TenantId = tenantContext.TenantId,
            MarinaId = command.MarinaId,
            DockId = command.DockId,
            Name = command.Name,
            SlipType = command.SlipType,
            MaxLength = command.MaxLength,
            MaxBeam = command.MaxBeam,
            MaxDraft = command.MaxDraft,
            HasElectric = command.HasElectric,
            Electric = command.Electric,
            HasWater = command.HasWater,
            RateType = command.RateType,
            DailyRate = command.DailyRate,
            MonthlyRate = command.MonthlyRate,
            AnnualRate = command.AnnualRate,
            Status = command.Status,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            Notes = command.Notes,
        };

        db.Slips.Add(slip);
        await db.SaveChangesAsync(ct);

        return slip.Id;
    }
}
