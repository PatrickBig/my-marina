using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class CreateSlipCommandHandler(AppDbContext db)
    : ICommandHandler<CreateSlipCommand, SlipDto>
{
    public async Task<SlipDto> HandleAsync(CreateSlipCommand command, CancellationToken ct = default)
    {
        var slip = new Slip
        {
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
            Notes = command.Notes,
        };
        db.Slips.Add(slip);
        await db.SaveChangesAsync(ct);
        return MarinaMappers.ToSlipDto(slip);
    }
}
