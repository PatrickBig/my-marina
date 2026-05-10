using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class UpdateSlipCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateSlipCommand, SlipDto>
{
    public async Task<SlipDto> HandleAsync(UpdateSlipCommand command, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Slip not found.");

        if (command.DockId is not null) slip.DockId = command.DockId;
        if (command.Name is not null) slip.Name = command.Name;
        if (command.SlipType.HasValue) slip.SlipType = command.SlipType.Value;
        if (command.MaxLength.HasValue) slip.MaxLength = command.MaxLength.Value;
        if (command.MaxBeam.HasValue) slip.MaxBeam = command.MaxBeam.Value;
        if (command.MaxDraft.HasValue) slip.MaxDraft = command.MaxDraft.Value;
        if (command.HasElectric.HasValue) slip.HasElectric = command.HasElectric.Value;
        if (command.Electric is not null) slip.Electric = command.Electric;
        if (command.HasWater.HasValue) slip.HasWater = command.HasWater.Value;
        if (command.HasPumpOut.HasValue) slip.HasPumpOut = command.HasPumpOut.Value;
        if (command.IsCovered.HasValue) slip.IsCovered = command.IsCovered.Value;
        if (command.IsIndoor.HasValue) slip.IsIndoor = command.IsIndoor.Value;
        if (command.Amenities is not null) slip.Amenities = [.. command.Amenities];
        if (command.Status.HasValue) slip.Status = command.Status.Value;
        if (command.Notes is not null) slip.Notes = command.Notes;

        if (command.ClearTransientRate)
        {
            slip.DefaultTransientRateKind = null;
            slip.DefaultTransientBaseRate = null;
            slip.DefaultTransientMinCharge = null;
        }
        else if (command.DefaultTransientBaseRate.HasValue
              && Enum.TryParse<RateKind>(command.DefaultTransientRateKind, ignoreCase: true, out var transientKind))
        {
            slip.DefaultTransientRateKind = transientKind;
            slip.DefaultTransientBaseRate = command.DefaultTransientBaseRate.Value;
            slip.DefaultTransientMinCharge = command.DefaultTransientMinCharge;
        }

        if (command.ClearLeaseRate)
        {
            slip.DefaultLeaseRateKind = null;
            slip.DefaultLeaseBaseRate = null;
            slip.DefaultLeaseTerm = null;
        }
        else if (command.DefaultLeaseBaseRate.HasValue
              && Enum.TryParse<RateKind>(command.DefaultLeaseRateKind, ignoreCase: true, out var leaseKind)
              && Enum.TryParse<LeaseTerm>(command.DefaultLeaseTerm, ignoreCase: true, out var leaseTerm))
        {
            slip.DefaultLeaseRateKind = leaseKind;
            slip.DefaultLeaseBaseRate = command.DefaultLeaseBaseRate.Value;
            slip.DefaultLeaseTerm = leaseTerm;
        }

        await db.SaveChangesAsync(ct);
        return MarinaMappers.ToSlipDto(slip);
    }
}
