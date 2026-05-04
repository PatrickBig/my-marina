using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Sublet;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Sublet;

public class DeleteOwnerAbsenceCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteOwnerAbsenceCommand, bool>
{
    public async Task<bool> HandleAsync(DeleteOwnerAbsenceCommand command, CancellationToken ct = default)
    {
        var absence = await db.OwnerAbsences
            .Include(a => a.Assignment)
            .FirstOrDefaultAsync(a =>
                a.Id == command.Id &&
                a.SlipAssignmentId == command.SlipAssignmentId &&
                command.RequestingUserBillingAccountIds.Contains(a.Assignment.BillingAccountId), ct)
            ?? throw new KeyNotFoundException("Absence not found or access denied.");

        db.OwnerAbsences.Remove(absence);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
