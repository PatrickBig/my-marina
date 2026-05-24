using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class RenewSlipAssignmentCommandHandler(AppDbContext db)
    : ICommandHandler<RenewSlipAssignmentCommand, SlipAssignmentDto>
{
    public async Task<SlipAssignmentDto> HandleAsync(RenewSlipAssignmentCommand command, CancellationToken ct = default)
    {
        var existing = await db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.BillingAccount)
            .Include(a => a.Vessel)
            .FirstOrDefaultAsync(a => a.Id == command.ExistingAssignmentId && a.Slip!.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"SlipAssignment {command.ExistingAssignmentId} not found.");

        var resolvedRate = existing.Slip!.ResolvedLeaseBaseRate;
        if (resolvedRate is null)
            throw new InvalidOperationException(
                $"Slip {existing.Slip!.Name} does not have a resolved lease rate. " +
                "Assign a pricing plan with lease rates configured before renewing.");

        // Conflict detection — exclude the existing assignment when checking
        var conflicts = await db.SlipAssignments
            .Where(a => a.SlipId == existing.SlipId && a.Id != existing.Id)
            .ToListAsync(ct);

        var overlap = conflicts.FirstOrDefault(a =>
            SlipAssignmentHelper.DateRangesOverlap(command.NewStartDate, command.NewEndDate, a.StartDate, a.EndDate));

        if (overlap is not null)
            throw new InvalidOperationException(
                $"Slip {existing.Slip!.Name} has a conflicting assignment for the renewal period " +
                $"({overlap.StartDate:d} – {(overlap.EndDate.HasValue ? overlap.EndDate.Value.ToString("d") : "open-ended")}).");

        var renewal = new SlipAssignment
        {
            SlipId                   = existing.SlipId,
            BillingAccountId         = existing.BillingAccountId,
            VesselId                 = existing.VesselId,
            AssignmentType           = existing.AssignmentType,
            StartDate                = command.NewStartDate,
            EndDate                  = command.NewEndDate,
            BaseRate                 = resolvedRate!.Value,
            AllowOwnerSubletWhenAway = existing.AllowOwnerSubletWhenAway,
            AllowHolderSublet        = existing.AllowHolderSublet,
            OwnerSubletShareToHolder = existing.OwnerSubletShareToHolder,
            HolderSubletShareToOwner = existing.HolderSubletShareToOwner,
            Notes                    = existing.Notes,
        };

        db.SlipAssignments.Add(renewal);
        await db.SaveChangesAsync(ct);

        renewal.Slip           = existing.Slip;
        renewal.BillingAccount = existing.BillingAccount;
        renewal.Vessel         = existing.Vessel;

        return SlipAssignmentHelper.ToDto(renewal);
    }
}
