using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Vessels;

public class ClaimVesselCommandHandler(AppDbContext db)
    : ICommandHandler<ClaimVesselCommand, VesselDto>
{
    public async Task<VesselDto> HandleAsync(ClaimVesselCommand command, CancellationToken ct = default)
    {
        var normalizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.VesselId && v.OwnerUserId == null, ct)
            ?? throw new KeyNotFoundException($"Vessel {command.VesselId} not found or already claimed.");

        if (!string.Equals(vessel.ClaimEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The claim email does not match your account email.");

        vessel.OwnerUserId = command.UserId;
        vessel.ClaimedAt   = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return CreateVesselCommandHandler.ToDto(vessel);
    }
}
