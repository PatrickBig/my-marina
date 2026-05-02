using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class DeclineReservationCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService email)
    : ICommandHandler<DeclineReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(DeclineReservationCommand command, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .FirstOrDefaultAsync(r => r.Id == command.Id, ct)
            ?? throw new KeyNotFoundException($"Reservation {command.Id} not found.");

        if (reservation.Status is not (ReservationStatus.PendingApproval or ReservationStatus.PendingHostMarinaApproval))
            throw new InvalidOperationException($"Reservation in status {reservation.Status} cannot be declined.");

        // Validate the declining marina is the correct one for this status
        var expectedMarinaId = reservation.Status == ReservationStatus.PendingHostMarinaApproval
            ? reservation.Slip.HostMarinaId
            : reservation.Slip.MarinaId;

        if (expectedMarinaId != command.MarinaId)
            throw new InvalidOperationException("You are not authorized to decline this reservation.");

        reservation.Status     = ReservationStatus.Declined;
        reservation.DeclinedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        var marina = await db.Marinas.FindAsync([reservation.Slip.MarinaId], ct);
        var marinaName = marina?.Name ?? string.Empty;

        var boater = await userManager.FindByIdAsync(reservation.BoaterUserId.ToString());
        if (!string.IsNullOrEmpty(boater?.Email))
        {
            await email.SendReservationDeclinedAsync(
                boater.Email, reservation.Slip.Name, marinaName,
                reservation.ArrivesAt, reservation.DepartsAt, reservation.Id, ct);
        }

        return ReservationHelper.ToDtoWithMarina(reservation, marinaName);
    }
}
