using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class GetSlipAssignmentQueryHandler(AppDbContext db)
    : IQueryHandler<GetSlipAssignmentQuery, SlipAssignmentDto>
{
    public async Task<SlipAssignmentDto> HandleAsync(GetSlipAssignmentQuery query, CancellationToken ct = default)
    {
        var assignment = await db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.BillingAccount)
            .Include(a => a.Vessel)
            .FirstOrDefaultAsync(a => a.Id == query.Id && a.Slip.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException($"SlipAssignment {query.Id} not found.");

        return SlipAssignmentHelper.ToDto(assignment);
    }
}
