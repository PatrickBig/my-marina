using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Slips;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Slips;

public class GetAvailableSlipsQueryHandler(AppDbContext db) : IQueryHandler<GetAvailableSlipsQuery, IReadOnlyList<SlipDto>>
{
    public async Task<IReadOnlyList<SlipDto>> HandleAsync(GetAvailableSlipsQuery query, CancellationToken ct = default)
    {
        // Find slip IDs that have a conflicting assignment for the requested date range.
        // SlipAssignment doesn't have MarinaId directly, so we scope via Slips first.
        var marinaSlipIds = await db.Slips
            .Where(s => s.MarinaId == query.MarinaId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var conflictingSlipIds = await db.SlipAssignments
            .Where(a =>
                marinaSlipIds.Contains(a.SlipId) &&
                (a.EndDate == null || a.EndDate >= query.StartDate) &&
                a.StartDate <= (query.EndDate ?? DateOnly.MaxValue))
            .Select(a => a.SlipId)
            .Distinct()
            .ToListAsync(ct);

        return await db.Slips
            .Where(s =>
                s.MarinaId == query.MarinaId &&
                s.Status == SlipStatus.Available &&
                s.MaxLength >= query.BoatLength &&
                s.MaxBeam >= query.BoatBeam &&
                s.MaxDraft >= query.BoatDraft &&
                !conflictingSlipIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(s => new SlipDto(
                s.Id, s.MarinaId, s.DockId, s.Name, s.SlipType,
                s.MaxLength, s.MaxBeam, s.MaxDraft,
                s.HasElectric, s.Electric, s.HasWater,
                s.RateType, s.DailyRate, s.MonthlyRate, s.AnnualRate,
                s.Status, s.Latitude, s.Longitude, s.Notes, s.CreatedAt))
            .ToListAsync(ct);
    }
}
