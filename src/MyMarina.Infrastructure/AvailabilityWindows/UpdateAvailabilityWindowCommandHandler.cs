using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.AvailabilityWindows;

public class UpdateAvailabilityWindowCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateAvailabilityWindowCommand, AvailabilityWindowDto>
{
    public async Task<AvailabilityWindowDto> HandleAsync(UpdateAvailabilityWindowCommand command, CancellationToken ct = default)
    {
        var window = await db.AvailabilityWindows
            .Include(w => w.Slip)
            .FirstOrDefaultAsync(w => w.Id == command.Id && w.Slip.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"AvailabilityWindow {command.Id} not found.");

        var newStart = command.StartsAt ?? window.StartsAt;
        var newEnd   = command.EndsAt   ?? window.EndsAt;

        if (newStart >= newEnd)
            throw new InvalidOperationException("StartsAt must be before EndsAt.");

        // Re-check non-overlap when dates change, excluding this window
        if (command.StartsAt.HasValue || command.EndsAt.HasValue)
        {
            var others = await db.AvailabilityWindows
                .Where(w => w.SlipId == window.SlipId && w.Id != window.Id)
                .ToListAsync(ct);

            var overlap = others.FirstOrDefault(w =>
                AvailabilityWindowHelper.DateRangesOverlap(newStart, newEnd, w.StartsAt, w.EndsAt));

            if (overlap is not null)
                throw new InvalidOperationException(
                    $"Updated dates overlap an existing listing window ({overlap.StartsAt:d} – {overlap.EndsAt:d}).");
        }

        window.StartsAt          = newStart;
        window.EndsAt            = newEnd;
        window.InstantBook       = command.InstantBook       ?? window.InstantBook;
        window.MinNights         = command.MinNights         ?? window.MinNights;
        window.MaxNights         = command.MaxNights         ?? window.MaxNights;
        window.BasePricePerNight = command.BasePricePerNight ?? window.BasePricePerNight;
        window.WeeklyDiscount    = command.WeeklyDiscount    ?? window.WeeklyDiscount;
        window.MonthlyDiscount   = command.MonthlyDiscount   ?? window.MonthlyDiscount;
        window.CleaningFee       = command.CleaningFee       ?? window.CleaningFee;

        await db.SaveChangesAsync(ct);
        return AvailabilityWindowHelper.ToDto(window);
    }
}
