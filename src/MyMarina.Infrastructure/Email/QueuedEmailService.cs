using MyMarina.Application.Abstractions;
using MyMarina.Infrastructure.Email.Templates;

namespace MyMarina.Infrastructure.Email;

public sealed class QueuedEmailService(IMessageBus messageBus) : IEmailService
{
    public Task SendEmailConfirmationAsync(string toEmail, string userId, string token,
        CancellationToken ct = default)
    {
        const string subject = "Confirm your MyMarina email address";
        var body = EmailTemplates.EmailConfirmation(toEmail, userId, token);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, toEmail, subject, body), ct);
    }

    public Task SendPasswordResetAsync(string toEmail, string userId, string token,
        CancellationToken ct = default)
    {
        const string subject = "Reset your MyMarina password";
        var body = EmailTemplates.PasswordReset(toEmail, userId, token);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, toEmail, subject, body), ct);
    }

    public Task SendMembershipInviteAsync(string toEmail, string marinaName, string invitedByName,
        Guid membershipId, CancellationToken ct = default)
    {
        var subject = $"You've been invited to join {marinaName} on MyMarina";
        var body = EmailTemplates.MembershipInvite(toEmail, marinaName, invitedByName, membershipId);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, toEmail, subject, body), ct);
    }
}
