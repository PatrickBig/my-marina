using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class CreateSlipAssignmentCommandHandler(AppDbContext db)
    : ICommandHandler<CreateSlipAssignmentCommand, SlipAssignmentDto>
{
    public async Task<SlipAssignmentDto> HandleAsync(CreateSlipAssignmentCommand command, CancellationToken ct = default)
    {
        // Verify slip belongs to this marina
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found.");

        // Verify billing account belongs to this marina
        var billingAccount = await db.BillingAccounts
            .FirstOrDefaultAsync(b => b.Id == command.BillingAccountId && b.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"BillingAccount {command.BillingAccountId} not found.");

        // Vessel-fit check
        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.VesselId, ct)
            ?? throw new KeyNotFoundException($"Vessel {command.VesselId} not found.");

        if (vessel.Length > slip.MaxLength || vessel.Beam > slip.MaxBeam || vessel.Draft > slip.MaxDraft)
            throw new InvalidOperationException(
                $"Vessel does not fit slip {slip.Name}. " +
                $"Vessel: {vessel.Length}L × {vessel.Beam}B × {vessel.Draft}D. " +
                $"Slip max: {slip.MaxLength}L × {slip.MaxBeam}B × {slip.MaxDraft}D.");

        // Conflict detection — no overlapping active assignments on the same slip
        var conflicts = await db.SlipAssignments
            .Where(a => a.SlipId == command.SlipId)
            .ToListAsync(ct);

        var overlap = conflicts.FirstOrDefault(a =>
            SlipAssignmentHelper.DateRangesOverlap(command.StartDate, command.EndDate, a.StartDate, a.EndDate));

        if (overlap is not null)
            throw new InvalidOperationException(
                $"Slip {slip.Name} is already assigned for the requested date range " +
                $"({overlap.StartDate:d} – {(overlap.EndDate.HasValue ? overlap.EndDate.Value.ToString("d") : "open-ended")}).");

        var assignment = new SlipAssignment
        {
            SlipId                   = command.SlipId,
            BillingAccountId         = command.BillingAccountId,
            VesselId                 = command.VesselId,
            AssignmentType           = command.AssignmentType,
            StartDate                = command.StartDate,
            EndDate                  = command.EndDate,
            BaseRate                 = command.BaseRate,
            AllowOwnerSubletWhenAway = command.AllowOwnerSubletWhenAway,
            AllowHolderSublet        = command.AllowHolderSublet,
            OwnerSubletShareToHolder = command.OwnerSubletShareToHolder,
            HolderSubletShareToOwner = command.HolderSubletShareToOwner,
            Notes                    = command.Notes,
        };

        db.SlipAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        // Reload with nav props for the DTO
        assignment.Slip           = slip;
        assignment.BillingAccount = billingAccount;
        assignment.Vessel         = vessel;

        return SlipAssignmentHelper.ToDto(assignment);
    }
}
