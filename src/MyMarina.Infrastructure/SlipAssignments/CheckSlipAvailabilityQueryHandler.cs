using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class CheckSlipAvailabilityQueryHandler(AppDbContext db)
    : IQueryHandler<CheckSlipAvailabilityQuery, SlipAvailabilityResult>
{
    public async Task<SlipAvailabilityResult> HandleAsync(CheckSlipAvailabilityQuery query, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == query.SlipId && s.MarinaId == query.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Slip {query.SlipId} not found.");

        var conflicts = new List<SlipAssignmentConflict>();

        // Vessel-fit check
        if (query.VesselLength.HasValue && query.VesselLength > slip.MaxLength ||
            query.VesselBeam.HasValue   && query.VesselBeam   > slip.MaxBeam   ||
            query.VesselDraft.HasValue  && query.VesselDraft  > slip.MaxDraft)
        {
            return new SlipAvailabilityResult(false, conflicts);
        }

        // Date-range conflict check
        var existing = await db.SlipAssignments
            .Include(a => a.BillingAccount)
            .Where(a => a.SlipId == query.SlipId
                     && (query.ExcludeAssignmentId == null || a.Id != query.ExcludeAssignmentId))
            .ToListAsync(ct);

        foreach (var a in existing)
        {
            if (SlipAssignmentHelper.DateRangesOverlap(query.StartDate, query.EndDate, a.StartDate, a.EndDate))
            {
                conflicts.Add(new SlipAssignmentConflict(
                    a.Id,
                    a.BillingAccount?.DisplayName ?? string.Empty,
                    a.StartDate,
                    a.EndDate));
            }
        }

        return new SlipAvailabilityResult(conflicts.Count == 0, conflicts);
    }
}
