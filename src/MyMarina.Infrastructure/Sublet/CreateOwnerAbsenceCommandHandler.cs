using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Sublet;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Sublet;

public class CreateOwnerAbsenceCommandHandler(AppDbContext db)
    : ICommandHandler<CreateOwnerAbsenceCommand, OwnerAbsenceDto>
{
    public async Task<OwnerAbsenceDto> HandleAsync(CreateOwnerAbsenceCommand command, CancellationToken ct = default)
    {
        if (command.StartsOn >= command.EndsOn)
            throw new InvalidOperationException("StartsOn must be before EndsOn.");

        var assignment = await db.SlipAssignments
            .Include(a => a.Slip)
            .FirstOrDefaultAsync(a =>
                a.Id == command.SlipAssignmentId &&
                command.RequestingUserBillingAccountIds.Contains(a.BillingAccountId), ct)
            ?? throw new KeyNotFoundException("Slip assignment not found or access denied.");

        // Prevent overlap with existing absences on this assignment
        var overlap = await db.OwnerAbsences
            .AnyAsync(x =>
                x.SlipAssignmentId == command.SlipAssignmentId &&
                x.StartsOn < command.EndsOn &&
                command.StartsOn < x.EndsOn, ct);

        if (overlap)
            throw new InvalidOperationException("An absence already covers part of the requested date range.");

        var absence = new OwnerAbsence
        {
            SlipAssignmentId = command.SlipAssignmentId,
            SlipId           = assignment.SlipId,
            StartsOn         = command.StartsOn,
            EndsOn           = command.EndsOn,
            Notes            = command.Notes,
        };

        db.OwnerAbsences.Add(absence);
        await db.SaveChangesAsync(ct);

        return SubletHelper.ToDto(absence, assignment.Slip.Name);
    }
}
