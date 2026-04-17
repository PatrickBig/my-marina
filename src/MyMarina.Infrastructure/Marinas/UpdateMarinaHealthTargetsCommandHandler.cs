using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class UpdateMarinaHealthTargetsCommandHandler(AppDbContext db) : ICommandHandler<UpdateMarinaHealthTargetsCommand>
{
    public async Task HandleAsync(UpdateMarinaHealthTargetsCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas.FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Marina {command.MarinaId} not found.");

        marina.HealthTargets = new HealthTargets(
            OccupancyRateTarget: command.HealthTargets.OccupancyRateTarget,
            OverdueARThresholdDays: command.HealthTargets.OverdueARThresholdDays,
            TargetMonthlyRevenue: command.HealthTargets.TargetMonthlyRevenue);
        marina.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
