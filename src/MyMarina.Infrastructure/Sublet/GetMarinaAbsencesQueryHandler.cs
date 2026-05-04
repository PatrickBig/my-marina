using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Sublet;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Sublet;

public class GetMarinaAbsencesQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaAbsencesQuery, IReadOnlyList<OwnerAbsenceDto>>
{
    public async Task<IReadOnlyList<OwnerAbsenceDto>> HandleAsync(
        GetMarinaAbsencesQuery query, CancellationToken ct = default)
    {
        // Slips owned by this marina
        var slipIds = await db.Slips
            .Where(s => s.MarinaId == query.MarinaId &&
                        (query.SlipId == null || s.Id == query.SlipId))
            .Select(s => s.Id)
            .ToListAsync(ct);

        // Assignments for those slips
        var assignmentIds = await db.SlipAssignments
            .Where(a => slipIds.Contains(a.SlipId))
            .Select(a => new { a.Id, a.SlipId })
            .ToListAsync(ct);

        if (assignmentIds.Count == 0) return [];

        var assignmentIdSet = assignmentIds.Select(x => x.Id).ToList();
        var slipIdByAssignment = assignmentIds.ToDictionary(x => x.Id, x => x.SlipId);

        var absences = await db.OwnerAbsences
            .Where(a => assignmentIdSet.Contains(a.SlipAssignmentId))
            .OrderBy(a => a.StartsOn)
            .ToListAsync(ct);

        if (absences.Count == 0) return [];

        var presentSlipIds = absences.Select(a => a.SlipId).Distinct().ToList();
        var slipNames = await db.Slips
            .Where(s => presentSlipIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        return absences
            .Select(a => SubletHelper.ToDto(a, slipNames.GetValueOrDefault(a.SlipId) ?? string.Empty))
            .ToList();
    }
}
