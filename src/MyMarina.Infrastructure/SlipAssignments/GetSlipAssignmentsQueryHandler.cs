using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class GetSlipAssignmentsQueryHandler(AppDbContext db)
    : IQueryHandler<GetSlipAssignmentsQuery, IReadOnlyList<SlipAssignmentDto>>
{
    public async Task<IReadOnlyList<SlipAssignmentDto>> HandleAsync(GetSlipAssignmentsQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var q = db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.BillingAccount)
            .Include(a => a.Vessel)
            .Where(a => a.Slip.MarinaId == query.MarinaId);

        if (query.SlipId.HasValue)
            q = q.Where(a => a.SlipId == query.SlipId.Value);

        if (query.BillingAccountId.HasValue)
            q = q.Where(a => a.BillingAccountId == query.BillingAccountId.Value);

        if (query.ActiveOnly)
            q = q.Where(a => a.EndDate == null || a.EndDate >= today);

        return await q
            .OrderByDescending(a => a.StartDate)
            .Select(a => SlipAssignmentHelper.ToDto(a))
            .ToListAsync(ct);
    }
}
