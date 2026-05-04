using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Sublet;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Sublet;

public class GetMySlipAssignmentsQueryHandler(AppDbContext db)
    : IQueryHandler<GetMySlipAssignmentsQuery, IReadOnlyList<MySlipAssignmentDto>>
{
    public async Task<IReadOnlyList<MySlipAssignmentDto>> HandleAsync(
        GetMySlipAssignmentsQuery query, CancellationToken ct = default)
    {
        if (query.BillingAccountIds.Count == 0) return [];

        var assignments = await db.SlipAssignments
            .Include(a => a.Slip)
            .Include(a => a.Vessel)
            .Where(a => query.BillingAccountIds.Contains(a.BillingAccountId))
            .OrderByDescending(a => a.StartDate)
            .ToListAsync(ct);

        if (assignments.Count == 0) return [];

        var marinaIds = assignments.Select(a => a.Slip.MarinaId).Distinct().ToList();
        var marinas   = await db.Marinas
            .Where(m => marinaIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        var today = DateOnly.FromDateTime(DateTime.Today);

        return assignments.Select(a => new MySlipAssignmentDto(
            Id:                     a.Id,
            SlipId:                 a.SlipId,
            SlipName:               a.Slip.Name,
            SlipType:               a.Slip.SlipType.ToString(),
            MarinaId:               a.Slip.MarinaId,
            MarinaName:             marinas.GetValueOrDefault(a.Slip.MarinaId) ?? string.Empty,
            BillingAccountId:       a.BillingAccountId,
            VesselId:               a.VesselId,
            VesselName:             a.Vessel.Name,
            AssignmentType:         a.AssignmentType.ToString(),
            StartDate:              a.StartDate,
            EndDate:                a.EndDate,
            BaseRate:               a.BaseRate,
            AllowHolderSublet:      a.AllowHolderSublet,
            AllowOwnerSubletWhenAway: a.AllowOwnerSubletWhenAway,
            IsActive:               a.EndDate == null || a.EndDate >= today
        )).ToList();
    }
}
