using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class CancelReservationCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService email)
    : ICommandHandler<CancelReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(CancelReservationCommand command, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new KeyNotFoundException($"Reservation {command.Id} not found.");

        if (reservation.Status is ReservationStatus.Declined
                                or ReservationStatus.Cancelled
                                or ReservationStatus.Completed
                                or ReservationStatus.NoShow)
            throw new InvalidOperationException($"Reservation in status {reservation.Status} cannot be cancelled.");

        var marina = await db.Marinas.FindAsync([reservation.Slip.MarinaId], ct);
        var marinaName = marina?.Name ?? string.Empty;

        // Requestor must be the boater OR have marina access (validated in controller)
        reservation.Status            = ReservationStatus.Cancelled;
        reservation.CancelledAt       = DateTimeOffset.UtcNow;
        reservation.CancelledByUserId = command.RequestingUserId;

        await db.SaveChangesAsync(ct);

        // Notify the other party
        var isCancelledByBoater = command.RequestingUserId == reservation.BoaterUserId;
        if (isCancelledByBoater && !string.IsNullOrEmpty(marina?.Email))
        {
            await email.SendReservationCancelledAsync(
                marina.Email, reservation.Slip.Name, marinaName,
                reservation.ArrivesAt, reservation.DepartsAt, reservation.Id, ct);
        }
        else
        {
            var boater = await userManager.FindByIdAsync(reservation.BoaterUserId.ToString());
            if (!string.IsNullOrEmpty(boater?.Email))
            {
                await email.SendReservationCancelledAsync(
                    boater.Email, reservation.Slip.Name, marinaName,
                    reservation.ArrivesAt, reservation.DepartsAt, reservation.Id, ct);
            }
        }

        return ReservationHelper.ToDtoWithMarina(reservation, marinaName);
    }
}
