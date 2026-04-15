namespace MyMarina.Application.Abstractions;

/// <summary>
/// Abstracts notification delivery — start with SMTP; add SendGrid/Twilio later.
/// </summary>
public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
