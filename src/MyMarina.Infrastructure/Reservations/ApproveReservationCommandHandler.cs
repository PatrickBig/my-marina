using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class ApproveReservationCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService email)
    : ICommandHandler<ApproveReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(ApproveReservationCommand command, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new KeyNotFoundException($"Reservation {command.Id} not found.");

        var marina = await db.Marinas.FindAsync([reservation.Slip.MarinaId], ct);
        var marinaName = marina?.Name ?? string.Empty;

        switch (reservation.Status)
        {
            case ReservationStatus.PendingApproval:
                // Slip owner (MarinaId) approves
                if (reservation.Slip.MarinaId != command.MarinaId)
                    throw new InvalidOperationException("Only the slip's marina can approve a PendingApproval reservation.");

                reservation.Status      = ReservationStatus.Confirmed;
                reservation.ConfirmedAt = DateTimeOffset.UtcNow;
                break;

            case ReservationStatus.PendingHostMarinaApproval:
                // Host marina (HostMarinaId) approves
                if (reservation.Slip.HostMarinaId != command.MarinaId)
                    throw new InvalidOperationException("Only the host marina can approve a PendingHostMarinaApproval reservation.");

                // After host-marina approval: if instant-book → Confirmed; else → PendingApproval (owner must also approve)
                reservation.Status = reservation.AvailabilityWindow.InstantBook
                    ? ReservationStatus.Confirmed
                    : ReservationStatus.PendingApproval;

                if (reservation.Status == ReservationStatus.Confirmed)
                    reservation.ConfirmedAt = DateTimeOffset.UtcNow;
                break;

            default:
                throw new InvalidOperationException($"Reservation in status {reservation.Status} cannot be approved.");
        }

        await db.SaveChangesAsync(ct);

        // Notify boater if now confirmed
        if (reservation.Status == ReservationStatus.Confirmed)
        {
            var boater = await userManager.FindByIdAsync(reservation.BoaterUserId.ToString());
            if (!string.IsNullOrEmpty(boater?.Email))
            {
                await email.SendReservationConfirmedAsync(
                    boater.Email, reservation.Slip.Name, marinaName,
                    reservation.ArrivesAt, reservation.DepartsAt, reservation.Total,
                    reservation.Id, ct);
            }
        }

        return ReservationHelper.ToDtoWithMarina(reservation, marinaName);
    }
}
