using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Leases;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

internal static class LeaseInquiryMappers
{
    internal static async Task<LeaseInquiryDto> ToDtoAsync(
        SlipLeaseInquiry i,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        CancellationToken ct)
    {
        var requester = await userManager.FindByIdAsync(i.RequestingUserId.ToString());
        var marina    = await db.Marinas.FindAsync([i.MarinaId], ct);

        var vessel = i.VesselId.HasValue
            ? await db.Vessels.FindAsync([i.VesselId.Value], ct)
            : null;

        string? reviewedByName  = await UserName(userManager, i.ReviewedByUserId);
        string? approvedByName  = await UserName(userManager, i.ApprovedByUserId);
        string? declinedByName  = await UserName(userManager, i.DeclinedByUserId);

        return new LeaseInquiryDto(
            Id: i.Id,
            SlipId: i.SlipId,
            SlipName: i.Slip.Name,
            MarinaId: i.MarinaId,
            MarinaName: marina?.Name ?? string.Empty,
            RequestingUserId: i.RequestingUserId,
            RequestingUserName: requester != null ? $"{requester.FirstName} {requester.LastName}".Trim() : "Unknown",
            RequestingUserEmail: requester?.Email ?? string.Empty,
            VesselId: i.VesselId,
            VesselName: vessel?.Name,
            DesiredTerm: i.DesiredTerm.ToString(),
            DesiredStartDate: i.DesiredStartDate,
            Message: i.Message,
            AgreedRateKind: i.AgreedRateKind?.ToString(),
            AgreedBaseRate: i.AgreedBaseRate,
            AssignmentStartDate: i.AssignmentStartDate,
            AssignmentEndDate: i.AssignmentEndDate,
            MarinaNote: i.MarinaNote,
            Status: i.Status.ToString(),
            ReviewedByUserName: reviewedByName,
            ReviewedAt: i.ReviewedAt,
            ApprovedByUserName: approvedByName,
            ApprovedAt: i.ApprovedAt,
            DeclinedByUserName: declinedByName,
            DeclinedAt: i.DeclinedAt,
            SlipAssignmentId: i.SlipAssignmentId,
            BillingAccountId: i.BillingAccountId,
            CreatedAt: i.CreatedAt
        );
    }

    private static async Task<string?> UserName(UserManager<ApplicationUser> um, Guid? userId)
    {
        if (!userId.HasValue) return null;
        var u = await um.FindByIdAsync(userId.Value.ToString());
        return u != null ? $"{u.FirstName} {u.LastName}".Trim() : null;
    }
}
