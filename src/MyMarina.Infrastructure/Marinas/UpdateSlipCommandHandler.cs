using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
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
        if (command.Status.HasValue) slip.Status = command.Status.Value;
        if (command.Notes is not null) slip.Notes = command.Notes;

        await db.SaveChangesAsync(ct);
        return MarinaMappers.ToSlipDto(slip);
    }
}
