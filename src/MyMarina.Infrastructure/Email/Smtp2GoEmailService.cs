using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MyMarina.Application.Abstractions;
using MyMarina.Infrastructure.Email.Templates;

namespace MyMarina.Infrastructure.Email;

public sealed class Smtp2GoEmailService(
    IOptions<EmailOptions> options,
    ILogger<Smtp2GoEmailService> logger) : IEmailService
{
    private readonly EmailOptions _opts = options.Value;

    public async Task SendCustomerInviteAsync(string toEmail, string customerName, string marinaName,
        string temporaryPassword, string confirmationLink, CancellationToken ct = default)
    {
        var subject = $"Welcome to {marinaName} — activate your account";
        var body = EmailTemplates.CustomerInvite(customerName, marinaName, temporaryPassword, confirmationLink);
        await SendAsync(toEmail, customerName, subject, body, ct);
    }

    public async Task SendStaffInviteAsync(string toEmail, string staffName, string marinaName,
        string role, string temporaryPassword, string confirmationLink, CancellationToken ct = default)
    {
        var subject = $"You've been invited to {marinaName}";
        var body = EmailTemplates.StaffInvite(staffName, marinaName, role, temporaryPassword, confirmationLink);
        await SendAsync(toEmail, staffName, subject, body, ct);
    }

    public async Task SendEmailConfirmationAsync(string toEmail, string recipientName,
        string confirmationLink, CancellationToken ct = default)
    {
        const string subject = "Confirm your MyMarina email address";
        var body = EmailTemplates.EmailConfirmation(recipientName, confirmationLink);
        await SendAsync(toEmail, recipientName, subject, body, ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_opts.Host, _opts.Port, _opts.GetSecureSocketOptions(), ct);
        await smtp.AuthenticateAsync(_opts.Username, _opts.Password, ct);
        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(quit: true, ct);

        logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
    }
}
