using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Application.Sublet;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.AvailabilityWindows;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Sublet;

public class CreateHolderSubletWindowCommandHandler(AppDbContext db)
    : ICommandHandler<CreateHolderSubletWindowCommand, AvailabilityWindowDto>
{
    public async Task<AvailabilityWindowDto> HandleAsync(
        CreateHolderSubletWindowCommand command, CancellationToken ct = default)
    {
        if (command.StartsAt >= command.EndsAt)
            throw new InvalidOperationException("StartsAt must be before EndsAt.");

        var assignment = await db.SlipAssignments
            .Include(a => a.Slip)
            .FirstOrDefaultAsync(a =>
                a.Id == command.SlipAssignmentId &&
                command.RequestingUserBillingAccountIds.Contains(a.BillingAccountId), ct)
            ?? throw new KeyNotFoundException("Slip assignment not found or access denied.");

        if (!assignment.AllowHolderSublet)
            throw new InvalidOperationException("Your lease does not permit holder-initiated subletting.");

        // Window must be within assignment dates
        var windowStart = DateOnly.FromDateTime(command.StartsAt.DateTime);
        var windowEnd   = DateOnly.FromDateTime(command.EndsAt.DateTime);

        if (windowStart < assignment.StartDate)
            throw new InvalidOperationException("Window cannot start before your lease start date.");

        if (assignment.EndDate.HasValue && windowEnd > assignment.EndDate.Value)
            throw new InvalidOperationException("Window cannot end after your lease end date.");

        // Non-overlap check
        var existing = await db.AvailabilityWindows
            .Where(w => w.SlipId == assignment.SlipId)
            .ToListAsync(ct);

        var overlap = existing.FirstOrDefault(w =>
            AvailabilityWindowHelper.DateRangesOverlap(command.StartsAt, command.EndsAt, w.StartsAt, w.EndsAt));

        if (overlap is not null)
            throw new InvalidOperationException(
                $"Slip {assignment.Slip.Name} already has a listing window overlapping the requested dates " +
                $"({overlap.StartsAt:d} – {overlap.EndsAt:d}).");

        // Revenue split: marina gets HolderSubletShareToOwner, holder keeps the rest
        var marinaId       = assignment.Slip.MarinaId;
        var holderShare    = 1m - assignment.HolderSubletShareToOwner;
        var revenueSplit   = new List<RevenueSplitEntry>
        {
            new() { PayeeKind = "SlipOwner", PayeeId = marinaId,                        Percent = assignment.HolderSubletShareToOwner },
            new() { PayeeKind = "Holder",    PayeeId = assignment.BillingAccountId,     Percent = holderShare },
        };

        var window = new AvailabilityWindow
        {
            SlipId                   = assignment.SlipId,
            ListedByKind             = ListedByKind.Holder,
            ListedByBillingAccountId = assignment.BillingAccountId,
            RelatedAssignmentId      = assignment.Id,
            StartsAt                 = command.StartsAt,
            EndsAt                   = command.EndsAt,
            InstantBook              = command.InstantBook,
            MinNights                = command.MinNights,
            MaxNights                = command.MaxNights,
            BasePricePerNight        = command.BasePricePerNight,
            WeeklyDiscount           = command.WeeklyDiscount,
            MonthlyDiscount          = command.MonthlyDiscount,
            CleaningFee              = command.CleaningFee,
            RevenueSplit             = revenueSplit,
        };

        db.AvailabilityWindows.Add(window);
        await db.SaveChangesAsync(ct);

        window.Slip = assignment.Slip;
        return AvailabilityWindowHelper.ToDto(window);
    }
}
