using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Vessels;

public class RejectVesselClaimCommandHandler(AppDbContext db)
    : ICommandHandler<RejectVesselClaimCommand>
{
    public async Task HandleAsync(RejectVesselClaimCommand command, CancellationToken ct = default)
    {
        var normalizedEmail = command.UserEmail.Trim().ToLowerInvariant();

        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.VesselId && v.OwnerUserId == null, ct)
            ?? throw new KeyNotFoundException($"Vessel {command.VesselId} not found or already claimed.");

        if (!string.Equals(vessel.ClaimEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The claim email does not match your account email.");

        // Clear the claim email so this vessel no longer surfaces as a pending claim for this user.
        // The marina retains the vessel record but will need to update the email to re-invite.
        vessel.ClaimEmail = null;

        await db.SaveChangesAsync(ct);
    }
}
