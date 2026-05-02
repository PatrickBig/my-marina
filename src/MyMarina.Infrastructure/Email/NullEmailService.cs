using Microsoft.Extensions.Logging;
using MyMarina.Application.Abstractions;

namespace MyMarina.Infrastructure.Email;

public sealed class NullEmailService(ILogger<NullEmailService> logger) : IEmailService
{
    public Task SendEmailConfirmationAsync(string toEmail, string userId, string token,
        CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: email confirmation to {Email} suppressed", toEmail);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string userId, string token,
        CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: password reset to {Email} suppressed", toEmail);
        return Task.CompletedTask;
    }

    public Task SendMembershipInviteAsync(string toEmail, string marinaName, string invitedByName,
        Guid membershipId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: membership invite to {Email} for marina {Marina} suppressed", toEmail, marinaName);
        return Task.CompletedTask;
    }

    public Task SendGhostVesselClaimAsync(string toEmail, string marinaName, string vesselName,
        Guid vesselId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: ghost vessel claim to {Email} for vessel {Vessel} suppressed", toEmail, vesselName);
        return Task.CompletedTask;
    }

    public Task SendBillingAccountInviteAsync(string toEmail, string marinaName, string invitedByName,
        Guid memberId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: billing account invite to {Email} for marina {Marina} suppressed", toEmail, marinaName);
        return Task.CompletedTask;
    }

    public Task SendReservationRequestAsync(string toHostEmail, string marinaName, string boaterName,
        string slipName, DateTimeOffset arrivesAt, DateTimeOffset departsAt,
        Guid reservationId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: reservation request to host {Email} for slip {Slip} suppressed", toHostEmail, slipName);
        return Task.CompletedTask;
    }

    public Task SendReservationConfirmedAsync(string toBoaterEmail, string slipName, string marinaName,
        DateTimeOffset arrivesAt, DateTimeOffset departsAt, decimal total,
        Guid reservationId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: reservation confirmed to {Email} for slip {Slip} suppressed", toBoaterEmail, slipName);
        return Task.CompletedTask;
    }

    public Task SendReservationDeclinedAsync(string toBoaterEmail, string slipName, string marinaName,
        DateTimeOffset arrivesAt, DateTimeOffset departsAt,
        Guid reservationId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: reservation declined to {Email} for slip {Slip} suppressed", toBoaterEmail, slipName);
        return Task.CompletedTask;
    }

    public Task SendReservationCancelledAsync(string toEmail, string slipName, string marinaName,
        DateTimeOffset arrivesAt, DateTimeOffset departsAt,
        Guid reservationId, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: reservation cancelled to {Email} for slip {Slip} suppressed", toEmail, slipName);
        return Task.CompletedTask;
    }
}
