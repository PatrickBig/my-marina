using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.SlipAssignments;

public class GetSlipAssignmentsQueryHandler(AppDbContext db) : IQueryHandler<GetSlipAssignmentsQuery, IReadOnlyList<SlipAssignmentDto>>
{
    public async Task<IReadOnlyList<SlipAssignmentDto>> HandleAsync(GetSlipAssignmentsQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.CustomerAccount)
            .Include(a => a.Boat)
            .Where(a =>
                (query.SlipId == null || a.SlipId == query.SlipId) &&
                (query.CustomerAccountId == null || a.CustomerAccountId == query.CustomerAccountId) &&
                (!query.ActiveOnly || a.EndDate == null || a.EndDate >= today))
            .OrderByDescending(a => a.StartDate)
            .Select(a => new SlipAssignmentDto(
                a.Id,
                a.SlipId,
                a.Slip.Name,
                a.CustomerAccountId,
                a.CustomerAccount.DisplayName,
                a.BoatId,
                a.Boat.Name,
                a.AssignmentType,
                a.StartDate,
                a.EndDate,
                a.RateOverride,
                a.Notes,
                a.CreatedAt))
            .ToListAsync(ct);
    }
}
