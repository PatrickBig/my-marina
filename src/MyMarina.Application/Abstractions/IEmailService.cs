namespace MyMarina.Application.Abstractions;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(
        string toEmail,
        string userId,
        string token,
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail,
        string userId,
        string token,
        CancellationToken ct = default);

    Task SendMembershipInviteAsync(
        string toEmail,
        string marinaName,
        string invitedByName,
        Guid membershipId,
        CancellationToken ct = default);
}
