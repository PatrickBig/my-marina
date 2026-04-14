using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Slips;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Slips;

public class UpdateSlipCommandHandler(AppDbContext db) : ICommandHandler<UpdateSlipCommand>
{
    public async Task HandleAsync(UpdateSlipCommand command, CancellationToken ct = default)
    {
        var slip = await db.Slips.FirstOrDefaultAsync(s => s.Id == command.SlipId, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found.");

        slip.Name = command.Name;
        slip.SlipType = command.SlipType;
        slip.MaxLength = command.MaxLength;
        slip.MaxBeam = command.MaxBeam;
        slip.MaxDraft = command.MaxDraft;
        slip.HasElectric = command.HasElectric;
        slip.Electric = command.Electric;
        slip.HasWater = command.HasWater;
        slip.RateType = command.RateType;
        slip.DailyRate = command.DailyRate;
        slip.MonthlyRate = command.MonthlyRate;
        slip.AnnualRate = command.AnnualRate;
        slip.Status = command.Status;
        slip.Latitude = command.Latitude;
        slip.Longitude = command.Longitude;
        slip.Notes = command.Notes;
        slip.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
