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

        if (command.AvailabilityWindowId.HasValue)
            return await BookViaWindow(command, ct);

        if (command.SlipId.HasValue)
            return await BookDirect(command, ct);

        throw new InvalidOperationException("Either AvailabilityWindowId or SlipId is required.");
    }

    private async Task<ReservationDto> BookViaWindow(CreateReservationCommand command, CancellationToken ct)
    {
        var window = await db.AvailabilityWindows
            .Include(w => w.Slip)
            .FirstOrDefaultAsync(w => w.Id == command.AvailabilityWindowId && w.Status == AvailabilityWindowStatus.Open, ct)
            ?? throw new KeyNotFoundException($"Availability window {command.AvailabilityWindowId} not found or not open.");

        if (window.StartsAt > command.ArrivesAt || window.EndsAt < command.DepartsAt)
            throw new InvalidOperationException(
                $"The requested dates are not covered by this listing window ({window.StartsAt:d} – {window.EndsAt:d}).");

        var nights = (int)(command.DepartsAt.Date - command.ArrivesAt.Date).TotalDays;
        if (window.MinNights.HasValue && nights < window.MinNights)
            throw new InvalidOperationException($"This listing requires a minimum of {window.MinNights} nights.");
        if (window.MaxNights.HasValue && nights > window.MaxNights)
            throw new InvalidOperationException($"This listing allows a maximum of {window.MaxNights} nights.");

        var slip = window.Slip;
        return await CreateReservation(command, window, slip, ct);
    }

    private async Task<ReservationDto> BookDirect(CreateReservationCommand command, CancellationToken ct)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.Status == SlipStatus.Active, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found or inactive.");

        if (slip.DefaultTransientBaseRate is null || slip.DefaultTransientRateKind is null)
            throw new InvalidOperationException("This slip does not have a direct booking rate configured.");

        // Check for conflicting assignment
        var assigned = await db.SlipAssignments.AnyAsync(
            a => a.SlipId == slip.Id
              && a.StartDate <= DateOnly.FromDateTime(command.DepartsAt.Date)
              && (a.EndDate == null || a.EndDate >= DateOnly.FromDateTime(command.ArrivesAt.Date)), ct);
        if (assigned)
            throw new InvalidOperationException("This slip is not available for those dates (it has a long-term assignment).");

        // Auto-create a transient AvailabilityWindow for the requested dates
        var window = new AvailabilityWindow
        {
            SlipId            = slip.Id,
            StartsAt          = command.ArrivesAt,
            EndsAt            = command.DepartsAt,
            InstantBook       = true,
            BasePricePerNight = slip.DefaultTransientBaseRate.Value,
            RateKind          = slip.DefaultTransientRateKind.Value,
            MinCharge         = slip.DefaultTransientMinCharge,
            ListingKind       = ListingKind.Transient,
            Status            = AvailabilityWindowStatus.Open,
            ListedByKind      = ListedByKind.Owner,
        };
        db.AvailabilityWindows.Add(window);
        await db.SaveChangesAsync(ct);

        // Reload with navigation
        window.Slip = slip;

        return await CreateReservation(command, window, slip, ct);
    }

    private async Task<ReservationDto> CreateReservation(
        CreateReservationCommand command,
        AvailabilityWindow window,
        Slip slip,
        CancellationToken ct)
    {
        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.VesselId && v.OwnerUserId == command.RequestingUserId && !v.IsArchived, ct)
            ?? throw new KeyNotFoundException($"Vessel {command.VesselId} not found or not owned by you.");

        var conflicting = await db.Reservations
            .AnyAsync(r => r.SlipId == slip.Id
                        && r.Status != ReservationStatus.Declined
                        && r.Status != ReservationStatus.Cancelled
                        && r.ArrivesAt < command.DepartsAt
                        && r.DepartsAt > command.ArrivesAt, ct);
        if (conflicting)
            throw new InvalidOperationException("This slip is already reserved for some or all of the requested dates.");

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
            Notes      = command.Notes,
            ConfirmedAt = status == ReservationStatus.Confirmed ? DateTimeOffset.UtcNow : null,
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        reservation.Vessel             = vessel;
        reservation.Slip               = slip;
        reservation.AvailabilityWindow = window;

        var marina = await db.Marinas.FindAsync([slip.MarinaId], ct);

        var boater = await userManager.FindByIdAsync(command.RequestingUserId.ToString());
        var boaterName  = boater != null ? $"{boater.FirstName} {boater.LastName}".Trim() : "A boater";
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
