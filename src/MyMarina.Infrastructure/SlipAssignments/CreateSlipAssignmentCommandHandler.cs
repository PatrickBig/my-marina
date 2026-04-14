using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class CreateSlipAssignmentCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateSlipAssignmentCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateSlipAssignmentCommand command, CancellationToken ct = default)
    {
        // Conflict detection: slip must not have an overlapping active assignment
        var hasConflict = await db.SlipAssignments.AnyAsync(a =>
            a.SlipId == command.SlipId &&
            (a.EndDate == null || a.EndDate >= command.StartDate) &&
            a.StartDate <= (command.EndDate ?? DateOnly.MaxValue),
            ct);

        if (hasConflict)
            throw new InvalidOperationException("The slip already has an assignment that overlaps the requested date range.");

        // Validate the boat fits the slip
        var slip = await db.Slips.FirstOrDefaultAsync(s => s.Id == command.SlipId, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found.");

        var boat = await db.Boats.FirstOrDefaultAsync(b => b.Id == command.BoatId, ct)
            ?? throw new KeyNotFoundException($"Boat {command.BoatId} not found.");

        if (boat.Length > slip.MaxLength || boat.Beam > slip.MaxBeam || boat.Draft > slip.MaxDraft)
            throw new InvalidOperationException("The boat's dimensions exceed the slip's maximum allowed dimensions.");

        var assignment = new SlipAssignment
        {
            TenantId = tenantContext.TenantId,
            SlipId = command.SlipId,
            CustomerAccountId = command.CustomerAccountId,
            BoatId = command.BoatId,
            AssignmentType = command.AssignmentType,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            RateOverride = command.RateOverride,
            Notes = command.Notes,
        };

        db.SlipAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return assignment.Id;
    }
}
