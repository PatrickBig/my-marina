using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class SetupDocksCommandHandler(AppDbContext db)
    : ICommandHandler<SetupDocksCommand>
{
    public async Task HandleAsync(SetupDocksCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        if (marina.IsSetupComplete)
            throw new InvalidOperationException("Cannot replace dock structure on an active marina via the setup endpoint.");

        // Validate payload
        ValidateCommand(command);

        // Transactional bulk replace of the entire draft dock+slip tree
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var existingSlips = await db.Slips
            .Where(s => s.MarinaId == command.MarinaId)
            .ToListAsync(ct);
        db.Slips.RemoveRange(existingSlips);

        var existingDocks = await db.Docks
            .Where(d => d.MarinaId == command.MarinaId)
            .ToListAsync(ct);
        db.Docks.RemoveRange(existingDocks);

        await db.SaveChangesAsync(ct);

        int sortOrder = 0;
        foreach (var dockItem in command.Docks)
        {
            var dock = new Dock
            {
                MarinaId = command.MarinaId,
                Name = dockItem.Name.Trim(),
                SortOrder = sortOrder++,
            };
            db.Docks.Add(dock);

            foreach (var slipItem in dockItem.Slips)
            {
                if (!Enum.TryParse<SlipType>(slipItem.SlipType, ignoreCase: true, out var slipType))
                    slipType = SlipType.Floating;

                ElectricAmperage? electric = slipItem.Electric.HasValue
                    ? (ElectricAmperage)slipItem.Electric.Value
                    : null;

                var slip = new Slip
                {
                    MarinaId = command.MarinaId,
                    DockId = dock.Id,
                    Name = slipItem.Name.Trim(),
                    SlipType = slipType,
                    MaxLength = slipItem.MaxLength,
                    MaxBeam = slipItem.MaxBeam,
                    MaxDraft = slipItem.MaxDraft,
                    HasElectric = slipItem.HasElectric,
                    Electric = electric,
                    HasWater = slipItem.HasWater,
                    HasPumpOut = slipItem.HasPumpOut,
                    IsCovered = slipItem.IsCovered,
                    IsIndoor = slipItem.IsIndoor,
                    Amenities = [.. slipItem.Amenities],
                };
                db.Slips.Add(slip);
            }
        }

        marina.SetupStep = 3;
        marina.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static void ValidateCommand(SetupDocksCommand command)
    {
        if (command.Docks.Count == 0)
            return;

        var dockNames = command.Docks.Select(d => d.Name.Trim()).ToList();
        if (dockNames.Count != dockNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new ArgumentException("Dock names must be unique.");

        foreach (var dock in command.Docks)
        {
            if (string.IsNullOrWhiteSpace(dock.Name))
                throw new ArgumentException("Dock name cannot be empty.");

            var slipNames = dock.Slips.Select(s => s.Name.Trim()).ToList();
            if (slipNames.Count != slipNames.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new ArgumentException($"Slip names within dock '{dock.Name}' must be unique.");

            foreach (var slip in dock.Slips)
            {
                if (string.IsNullOrWhiteSpace(slip.Name))
                    throw new ArgumentException($"Slip name cannot be empty in dock '{dock.Name}'.");
                if (slip.MaxLength <= 0 || slip.MaxBeam <= 0 || slip.MaxDraft <= 0)
                    throw new ArgumentException($"Slip dimensions must be positive in dock '{dock.Name}', slip '{slip.Name}'.");
            }
        }
    }
}
