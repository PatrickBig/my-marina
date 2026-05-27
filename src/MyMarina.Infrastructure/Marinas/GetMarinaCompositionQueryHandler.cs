using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinaCompositionQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaCompositionQuery, MarinaCompositionDto>
{
    public async Task<MarinaCompositionDto> HandleAsync(GetMarinaCompositionQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var slipData = await db.Slips
            .Where(s => s.MarinaId == query.MarinaId && s.Status != SlipStatus.Inactive)
            .Select(s => new
            {
                s.Status,
                ActiveAssignmentType = db.SlipAssignments
                    .Where(a => a.SlipId == s.Id && (a.EndDate == null || a.EndDate >= today))
                    .Select(a => (AssignmentType?)a.AssignmentType)
                    .FirstOrDefault(),
                HasActiveWindow = db.AvailabilityWindows
                    .Any(w => w.SlipId == s.Id &&
                              (w.Status == AvailabilityWindowStatus.Open ||
                               w.Status == AvailabilityWindowStatus.Paused ||
                               w.Status == AvailabilityWindowStatus.FullyBooked))
            })
            .ToListAsync(ct);

        return new MarinaCompositionDto(
            Total:       slipData.Count,
            Annual:      slipData.Count(s => s.ActiveAssignmentType == AssignmentType.Annual),
            Seasonal:    slipData.Count(s => s.ActiveAssignmentType == AssignmentType.Seasonal),
            Monthly:     slipData.Count(s => s.ActiveAssignmentType == AssignmentType.Monthly),
            Transient:   slipData.Count(s => s.ActiveAssignmentType == AssignmentType.Transient),
            Listed:      slipData.Count(s => s.ActiveAssignmentType == null && s.Status == SlipStatus.Active && s.HasActiveWindow),
            Maintenance: slipData.Count(s => s.Status == SlipStatus.UnderMaintenance),
            Vacant:      slipData.Count(s => s.ActiveAssignmentType == null && s.Status == SlipStatus.Active && !s.HasActiveWindow));
    }
}
