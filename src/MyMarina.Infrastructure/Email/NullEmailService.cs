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
}
