using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Sublet;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Sublet;

public class GetOwnerAbsencesQueryHandler(AppDbContext db)
    : IQueryHandler<GetOwnerAbsencesQuery, IReadOnlyList<OwnerAbsenceDto>>
{
    public async Task<IReadOnlyList<OwnerAbsenceDto>> HandleAsync(
        GetOwnerAbsencesQuery query, CancellationToken ct = default)
    {
        var assignment = await db.SlipAssignments
            .Include(a => a.Slip)
            .FirstOrDefaultAsync(a =>
                a.Id == query.SlipAssignmentId &&
                query.RequestingUserBillingAccountIds.Contains(a.BillingAccountId), ct)
            ?? throw new KeyNotFoundException("Slip assignment not found or access denied.");

        var absences = await db.OwnerAbsences
            .Where(a => a.SlipAssignmentId == query.SlipAssignmentId)
            .OrderBy(a => a.StartsOn)
            .ToListAsync(ct);

        return absences
            .Select(a => SubletHelper.ToDto(a, assignment.Slip.Name))
            .ToList();
    }
}
