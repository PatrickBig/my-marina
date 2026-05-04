using MyMarina.Application.SlipAssignments;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.SlipAssignments;

internal static class SlipAssignmentHelper
{
    internal static SlipAssignmentDto ToDto(SlipAssignment a) => new(
        Id:                        a.Id,
        SlipId:                    a.SlipId,
        SlipName:                  a.Slip?.Name ?? string.Empty,
        BillingAccountId:          a.BillingAccountId,
        BillingAccountDisplayName: a.BillingAccount?.DisplayName ?? string.Empty,
        VesselId:                  a.VesselId,
        VesselName:                a.Vessel?.Name ?? string.Empty,
        AssignmentType:            a.AssignmentType.ToString(),
        StartDate:                 a.StartDate,
        EndDate:                   a.EndDate,
        BaseRate:                  a.BaseRate,
        AllowOwnerSubletWhenAway:  a.AllowOwnerSubletWhenAway,
        AllowHolderSublet:         a.AllowHolderSublet,
        OwnerSubletShareToHolder:  a.OwnerSubletShareToHolder,
        HolderSubletShareToOwner:  a.HolderSubletShareToOwner,
        Notes:                     a.Notes,
        IsActive:                  a.EndDate == null || a.EndDate >= DateOnly.FromDateTime(DateTime.Today),
        CreatedAt:                 a.CreatedAt
    );

    /// <summary>
    /// Returns true when [s1,e1) and [s2,e2) overlap.
    /// A null EndDate means open-ended (no upper bound).
    /// Same-day start/end is treated as non-overlapping (end is exclusive).
    /// </summary>
    internal static bool DateRangesOverlap(DateOnly s1, DateOnly? e1, DateOnly s2, DateOnly? e2)
    {
        // [s1, e1) overlaps [s2, e2) when s1 < e2 AND s2 < e1
        // Open-ended = ∞ on the upper side
        bool startsBefore1 = e2 == null || s1 < e2.Value;
        bool startsBefore2 = e1 == null || s2 < e1.Value;
        return startsBefore1 && startsBefore2;
    }
}
