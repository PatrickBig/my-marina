using Microsoft.Extensions.Logging;
using MyMarina.Application.Abstractions;

namespace MyMarina.Infrastructure.Email;

public sealed class NullEmailService(ILogger<NullEmailService> logger) : IEmailService
{
    public Task SendCustomerInviteAsync(string toEmail, string customerName, string marinaName,
        string temporaryPassword, string confirmationLink, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: customer invite to {Email} suppressed", toEmail);
        return Task.CompletedTask;
    }

    public Task SendStaffInviteAsync(string toEmail, string staffName, string marinaName,
        string role, string temporaryPassword, string confirmationLink, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: staff invite to {Email} suppressed", toEmail);
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string toEmail, string recipientName,
        string confirmationLink, CancellationToken ct = default)
    {
        logger.LogDebug("NullEmailService: email confirmation to {Email} suppressed", toEmail);
        return Task.CompletedTask;
    }
}
