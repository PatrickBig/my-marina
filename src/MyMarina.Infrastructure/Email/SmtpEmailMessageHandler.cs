using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MyMarina.Application.Abstractions;

namespace MyMarina.Infrastructure.Email;

public sealed class SmtpEmailMessageHandler(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailMessageHandler> logger) : IMessageHandler<SendEmailMessage>
{
    private readonly EmailOptions _opts = options.Value;

    public async Task HandleAsync(SendEmailMessage message, CancellationToken ct)
    {
        var domain = message.ToEmail.Split('@').LastOrDefault() ?? string.Empty;
        if (_opts.ExcludedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogDebug("Skipping email to {Email}: domain is excluded", message.ToEmail);
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;
        mime.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.HtmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_opts.Host, _opts.Port, _opts.GetSecureSocketOptions(), ct);
        await smtp.AuthenticateAsync(_opts.Username, _opts.Password, ct);
        await smtp.SendAsync(mime, ct);
        await smtp.DisconnectAsync(quit: true, ct);

        logger.LogInformation("Email sent to {Email}: {Subject}", message.ToEmail, message.Subject);
    }
}
