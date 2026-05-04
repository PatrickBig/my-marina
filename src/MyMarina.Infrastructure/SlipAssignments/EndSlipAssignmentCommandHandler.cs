using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class EndSlipAssignmentCommandHandler(AppDbContext db)
    : ICommandHandler<EndSlipAssignmentCommand, SlipAssignmentDto>
{
    public async Task<SlipAssignmentDto> HandleAsync(EndSlipAssignmentCommand command, CancellationToken ct = default)
    {
        var assignment = await db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.BillingAccount)
            .Include(a => a.Vessel)
            .FirstOrDefaultAsync(a => a.Id == command.Id && a.Slip.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"SlipAssignment {command.Id} not found.");

        if (assignment.EndDate.HasValue && assignment.EndDate < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException("Assignment is already ended.");

        assignment.EndDate = command.EndDate;
        await db.SaveChangesAsync(ct);
        return SlipAssignmentHelper.ToDto(assignment);
    }
}
