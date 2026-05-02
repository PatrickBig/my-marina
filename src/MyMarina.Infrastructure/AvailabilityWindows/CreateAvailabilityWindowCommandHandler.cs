using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Domain.Entities;
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

        // Non-overlap enforcement — no two Open/Paused windows may share dates on the same slip
        var existing = await db.AvailabilityWindows
            .Where(w => w.SlipId == command.SlipId)
            .ToListAsync(ct);

        var overlap = existing.FirstOrDefault(w =>
            AvailabilityWindowHelper.DateRangesOverlap(command.StartsAt, command.EndsAt, w.StartsAt, w.EndsAt));

        if (overlap is not null)
            throw new InvalidOperationException(
                $"Slip {slip.Name} already has a listing window overlapping the requested dates " +
                $"({overlap.StartsAt:d} – {overlap.EndsAt:d}).");

        // Default revenue split: 100% to the slip owner (the marina)
        var revenueSplit = new List<RevenueSplitEntry>
        {
            new() { PayeeKind = "SlipOwner", PayeeId = command.MarinaId, Percent = 1.0m },
        };

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
