using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Leases;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

internal static class LeaseInquiryMappers
{
    /// <summary>
    /// Single-item convenience wrapper used by command handlers after mutations.
    /// Re-queries by ID so the returned DTO reflects the saved state.
    /// </summary>
    internal static async Task<LeaseInquiryDto> ToDtoAsync(
        SlipLeaseInquiry i,
        AppDbContext db,
        CancellationToken ct)
    {
        var results = await QueryAsync(db.SlipLeaseInquiries.Where(x => x.Id == i.Id), db, ct);
        return results.Single();
    }

    /// <summary>
    /// Projects a filtered, ordered inquiry queryable to DTOs in a single SQL query.
    /// Correlated subqueries on Users/Marinas/Vessels are translated to LEFT JOINs by EF Core.
    /// Enum-to-string mapping happens in memory after materialization.
    /// </summary>
    internal static async Task<IReadOnlyList<LeaseInquiryDto>> QueryAsync(
        IQueryable<SlipLeaseInquiry> q,
        AppDbContext db,
        CancellationToken ct)
    {
        var rows = await q
            .Select(i => new
            {
                i.Id,
                i.SlipId,
                SlipName = i.Slip.Name,
                i.MarinaId,
                MarinaName = db.Marinas
                    .Where(m => m.Id == i.MarinaId)
                    .Select(m => m.Name)
                    .FirstOrDefault(),
                i.RequestingUserId,
                RequesterName = db.Users
                    .Where(u => u.Id == i.RequestingUserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault(),
                RequesterEmail = db.Users
                    .Where(u => u.Id == i.RequestingUserId)
                    .Select(u => u.Email)
                    .FirstOrDefault(),
                i.VesselId,
                VesselName = db.Vessels
                    .Where(v => i.VesselId != null && v.Id == i.VesselId.Value)
                    .Select(v => v.Name)
                    .FirstOrDefault(),
                i.DesiredTerm,
                i.DesiredStartDate,
                i.Message,
                i.AgreedRateKind,
                i.AgreedBaseRate,
                i.AssignmentStartDate,
                i.AssignmentEndDate,
                i.MarinaNote,
                i.Status,
                ReviewedByName = db.Users
                    .Where(u => i.ReviewedByUserId != null && u.Id == i.ReviewedByUserId.Value)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault(),
                i.ReviewedAt,
                ApprovedByName = db.Users
                    .Where(u => i.ApprovedByUserId != null && u.Id == i.ApprovedByUserId.Value)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault(),
                i.ApprovedAt,
                DeclinedByName = db.Users
                    .Where(u => i.DeclinedByUserId != null && u.Id == i.DeclinedByUserId.Value)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault(),
                i.DeclinedAt,
                i.SlipAssignmentId,
                i.BillingAccountId,
                i.CreatedAt,
            })
            .ToListAsync(ct);

        return rows.Select(r => new LeaseInquiryDto(
            Id:                  r.Id,
            SlipId:              r.SlipId,
            SlipName:            r.SlipName,
            MarinaId:            r.MarinaId,
            MarinaName:          r.MarinaName ?? string.Empty,
            RequestingUserId:    r.RequestingUserId,
            RequestingUserName:  string.IsNullOrWhiteSpace(r.RequesterName) ? "Unknown" : r.RequesterName.Trim(),
            RequestingUserEmail: r.RequesterEmail ?? string.Empty,
            VesselId:            r.VesselId,
            VesselName:          r.VesselName,
            DesiredTerm:         r.DesiredTerm.ToString(),
            DesiredStartDate:    r.DesiredStartDate,
            Message:             r.Message,
            AgreedRateKind:      r.AgreedRateKind?.ToString(),
            AgreedBaseRate:      r.AgreedBaseRate,
            AssignmentStartDate: r.AssignmentStartDate,
            AssignmentEndDate:   r.AssignmentEndDate,
            MarinaNote:          r.MarinaNote,
            Status:              r.Status.ToString(),
            ReviewedByUserName:  r.ReviewedByName?.Trim() ?? string.Empty,
            ReviewedAt:          r.ReviewedAt,
            ApprovedByUserName:  r.ApprovedByName?.Trim() ?? string.Empty,
            ApprovedAt:          r.ApprovedAt,
            DeclinedByUserName:  r.DeclinedByName?.Trim() ?? string.Empty,
            DeclinedAt:          r.DeclinedAt,
            SlipAssignmentId:    r.SlipAssignmentId,
            BillingAccountId:    r.BillingAccountId,
            CreatedAt:           r.CreatedAt
        )).ToList();
    }
}
