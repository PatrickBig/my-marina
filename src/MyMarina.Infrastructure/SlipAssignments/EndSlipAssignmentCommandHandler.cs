using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class EndSlipAssignmentCommandHandler(AppDbContext db) : ICommandHandler<EndSlipAssignmentCommand>
{
    public async Task HandleAsync(EndSlipAssignmentCommand command, CancellationToken ct = default)
    {
        var assignment = await db.SlipAssignments.FirstOrDefaultAsync(a => a.Id == command.SlipAssignmentId, ct)
            ?? throw new KeyNotFoundException($"SlipAssignment {command.SlipAssignmentId} not found.");

        if (assignment.EndDate.HasValue && assignment.EndDate < command.EndDate)
            throw new InvalidOperationException("The provided end date is after the assignment's existing end date.");

        assignment.EndDate = command.EndDate;
        assignment.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
