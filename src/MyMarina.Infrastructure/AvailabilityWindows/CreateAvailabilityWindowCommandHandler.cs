using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.AvailabilityWindows;

public class CreateAvailabilityWindowCommandHandler(AppDbContext db)
    : ICommandHandler<CreateAvailabilityWindowCommand, AvailabilityWindowDto>
{
    public async Task<AvailabilityWindowDto> HandleAsync(CreateAvailabilityWindowCommand command, CancellationToken ct = default)
    {
        if (command.StartsAt >= command.EndsAt)
            throw new InvalidOperationException("StartsAt must be before EndsAt.");

        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found.");

        // Non-overlap enforcement
        var existing = await db.AvailabilityWindows
            .Where(w => w.SlipId == command.SlipId)
            .ToListAsync(ct);

        var overlap = existing.FirstOrDefault(w =>
            AvailabilityWindowHelper.DateRangesOverlap(command.StartsAt, command.EndsAt, w.StartsAt, w.EndsAt));

        if (overlap is not null)
            throw new InvalidOperationException(
                $"Slip {slip.Name} already has a listing window overlapping the requested dates " +
                $"({overlap.StartsAt:d} – {overlap.EndsAt:d}).");

        List<RevenueSplitEntry> revenueSplit;

        if (command.ListedByKind == ListedByKind.OwnerForHolder)
        {
            // Marina lists during the holder's recorded absence
            if (command.RelatedAssignmentId is null)
                throw new InvalidOperationException("RelatedAssignmentId is required for OwnerForHolder windows.");

            var assignment = await db.SlipAssignments
                .FirstOrDefaultAsync(a => a.Id == command.RelatedAssignmentId && a.SlipId == command.SlipId, ct)
                ?? throw new KeyNotFoundException($"Slip assignment {command.RelatedAssignmentId} not found.");

            if (!assignment.AllowOwnerSubletWhenAway)
                throw new InvalidOperationException(
                    "The holder's lease does not permit the marina to sublet during absences.");

            var windowStart = DateOnly.FromDateTime(command.StartsAt.DateTime);
            var windowEnd   = DateOnly.FromDateTime(command.EndsAt.DateTime);

            var hasAbsence = await db.OwnerAbsences
                .AnyAsync(a =>
                    a.SlipAssignmentId == command.RelatedAssignmentId &&
                    a.StartsOn <= windowStart &&
                    a.EndsOn   >= windowEnd, ct);

            if (!hasAbsence)
                throw new InvalidOperationException(
                    "No owner absence covers the requested window dates. " +
                    "The holder must first mark themselves away for this period.");

            revenueSplit =
            [
                new() { PayeeKind = "SlipOwner", PayeeId = command.MarinaId,                Percent = 1m - assignment.OwnerSubletShareToHolder },
                new() { PayeeKind = "Holder",    PayeeId = assignment.BillingAccountId,     Percent = assignment.OwnerSubletShareToHolder },
            ];
        }
        else
        {
            // ListedByKind.Owner — 100% to the slip owner (the marina)
            revenueSplit =
            [
                new() { PayeeKind = "SlipOwner", PayeeId = command.MarinaId, Percent = 1.0m },
            ];
        }

        var window = new AvailabilityWindow
        {
            SlipId                   = command.SlipId,
            ListedByKind             = command.ListedByKind,
            ListedByMarinaId         = command.ListedByMarinaId,
            ListedByBillingAccountId = command.ListedByBillingAccountId,
            RelatedAssignmentId      = command.RelatedAssignmentId,
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

        window.Slip = slip;
        return AvailabilityWindowHelper.ToDto(window);
    }
}
