using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class UpdateSlipAssignmentCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateSlipAssignmentCommand, SlipAssignmentDto>
{
    public async Task<SlipAssignmentDto> HandleAsync(UpdateSlipAssignmentCommand command, CancellationToken ct = default)
    {
        var assignment = await db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.BillingAccount)
            .Include(a => a.Vessel)
            .FirstOrDefaultAsync(a => a.Id == command.Id && a.Slip.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"SlipAssignment {command.Id} not found.");

        if (command.AssignmentType.HasValue)   assignment.AssignmentType           = command.AssignmentType.Value;
        if (command.StartDate.HasValue)        assignment.StartDate                = command.StartDate.Value;
        if (command.EndDate.HasValue)          assignment.EndDate                  = command.EndDate.Value;
        if (command.BaseRate.HasValue)         assignment.BaseRate                 = command.BaseRate.Value;
        if (command.AllowOwnerSubletWhenAway.HasValue) assignment.AllowOwnerSubletWhenAway = command.AllowOwnerSubletWhenAway.Value;
        if (command.AllowHolderSublet.HasValue) assignment.AllowHolderSublet       = command.AllowHolderSublet.Value;
        if (command.OwnerSubletShareToHolder.HasValue) assignment.OwnerSubletShareToHolder = command.OwnerSubletShareToHolder.Value;
        if (command.HolderSubletShareToOwner.HasValue) assignment.HolderSubletShareToOwner = command.HolderSubletShareToOwner.Value;
        if (command.Notes is not null)         assignment.Notes                    = command.Notes;

        await db.SaveChangesAsync(ct);
        return SlipAssignmentHelper.ToDto(assignment);
    }
}
