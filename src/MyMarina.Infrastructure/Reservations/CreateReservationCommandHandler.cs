using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class CreateReservationCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService email)
    : ICommandHandler<CreateReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(CreateReservationCommand command, CancellationToken ct = default)
    {
        if (command.ArrivesAt >= command.DepartsAt)
            throw new InvalidOperationException("Arrival must be before departure.");

        var window = await db.AvailabilityWindows
            .Include(w => w.Slip)
            .FirstOrDefaultAsync(w => w.Id == command.AvailabilityWindowId && w.Status == AvailabilityWindowStatus.Open, ct)
            ?? throw new KeyNotFoundException($"Availability window {command.AvailabilityWindowId} not found or not open.");

        // Window must cover the requested dates
        if (window.StartsAt > command.ArrivesAt || window.EndsAt < command.DepartsAt)
            throw new InvalidOperationException(
                $"The requested dates are not covered by this listing window ({window.StartsAt:d} – {window.EndsAt:d}).");

        // Min/max nights check
        var nights = (int)(command.DepartsAt.Date - command.ArrivesAt.Date).TotalDays;
        if (window.MinNights.HasValue && nights < window.MinNights)
            throw new InvalidOperationException($"This listing requires a minimum of {window.MinNights} nights.");
        if (window.MaxNights.HasValue && nights > window.MaxNights)
            throw new InvalidOperationException($"This listing allows a maximum of {window.MaxNights} nights.");

        var slip = window.Slip;

        // Boater's vessel must belong to them
        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.VesselId && v.OwnerUserId == command.RequestingUserId && !v.IsArchived, ct)
            ?? throw new KeyNotFoundException($"Vessel {command.VesselId} not found or not owned by you.");

        // Conflict check — no active reservation on this slip during the dates
        var conflicting = await db.Reservations
            .AnyAsync(r => r.SlipId == slip.Id
                        && r.Status != ReservationStatus.Declined
                        && r.Status != ReservationStatus.Cancelled
                        && r.ArrivesAt < command.DepartsAt
                        && r.DepartsAt > command.ArrivesAt, ct);

        if (conflicting)
            throw new InvalidOperationException("This slip is already reserved for some or all of the requested dates.");

        // Determine initial status
        var status = slip.HostMarinaPolicy == HostMarinaPolicy.RequiresApproval
            ? ReservationStatus.PendingHostMarinaApproval
            : window.InstantBook
                ? ReservationStatus.Confirmed
                : ReservationStatus.PendingApproval;

        var (basePrice, fees, taxes, total) = ReservationHelper.ComputePrice(window, command.ArrivesAt, command.DepartsAt);

        var reservation = new Reservation
        {
            BoaterUserId         = command.RequestingUserId,
            VesselId             = command.VesselId,
            SlipId               = slip.Id,
            AvailabilityWindowId = window.Id,
            ArrivesAt            = command.ArrivesAt,
            DepartsAt            = command.DepartsAt,
            Status               = status,
            BasePrice            = basePrice,
            Fees                 = fees,
            Taxes                = taxes,
            Total                = total,
            RevenueSplitSnapshot = window.RevenueSplit.Select(e => new Domain.ValueObjects.RevenueSplitEntry
            {
                PayeeKind = e.PayeeKind,
                PayeeId   = e.PayeeId,
                Percent   = e.Percent,
            }).ToList(),
            Notes                = command.Notes,
            ConfirmedAt          = status == ReservationStatus.Confirmed ? DateTimeOffset.UtcNow : null,
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        reservation.Vessel            = vessel;
        reservation.Slip              = slip;
        reservation.AvailabilityWindow = window;

        var marina = await db.Marinas.FindAsync([slip.MarinaId], ct);

        // Send notifications
        var boater = await userManager.FindByIdAsync(command.RequestingUserId.ToString());
        var boaterName = boater != null ? $"{boater.FirstName} {boater.LastName}".Trim() : "A boater";
        var boaterEmail = boater?.Email ?? string.Empty;

        if (status == ReservationStatus.Confirmed && !string.IsNullOrEmpty(boaterEmail))
        {
            await email.SendReservationConfirmedAsync(
                boaterEmail, slip.Name, marina?.Name ?? string.Empty,
                command.ArrivesAt, command.DepartsAt, total, reservation.Id, ct);
        }
        else if (!string.IsNullOrEmpty(marina?.Email))
        {
            await email.SendReservationRequestAsync(
                marina.Email, marina.Name, boaterName, slip.Name,
                command.ArrivesAt, command.DepartsAt, reservation.Id, ct);
        }

        return ReservationHelper.ToDtoWithMarina(reservation, marina?.Name ?? string.Empty);
    }
}
