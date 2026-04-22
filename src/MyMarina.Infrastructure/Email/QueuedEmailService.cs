using MyMarina.Application.Abstractions;
using MyMarina.Infrastructure.Email.Templates;

namespace MyMarina.Infrastructure.Email;

/// <summary>
/// IEmailService implementation that enqueues email sends as Hangfire background jobs via IMessageBus.
/// The actual SMTP send happens in SmtpEmailMessageHandler on a worker thread.
/// </summary>
public sealed class QueuedEmailService(IMessageBus messageBus) : IEmailService
{
    public Task SendCustomerInviteAsync(string toEmail, string customerName, string marinaName,
        string temporaryPassword, string confirmationLink, CancellationToken ct = default)
    {
        var subject = $"Welcome to {marinaName} — activate your account";
        var body = EmailTemplates.CustomerInvite(customerName, marinaName, temporaryPassword, confirmationLink);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, customerName, subject, body), ct);
    }

    public Task SendStaffInviteAsync(string toEmail, string staffName, string marinaName,
        string role, string temporaryPassword, string confirmationLink, CancellationToken ct = default)
    {
        var subject = $"You've been invited to {marinaName}";
        var body = EmailTemplates.StaffInvite(staffName, marinaName, role, temporaryPassword, confirmationLink);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, staffName, subject, body), ct);
    }

    public Task SendEmailConfirmationAsync(string toEmail, string recipientName,
        string confirmationLink, CancellationToken ct = default)
    {
        const string subject = "Confirm your MyMarina email address";
        var body = EmailTemplates.EmailConfirmation(recipientName, confirmationLink);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, recipientName, subject, body), ct);
    }
}
