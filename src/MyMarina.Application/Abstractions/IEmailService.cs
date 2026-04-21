namespace MyMarina.Application.Abstractions;

public interface IEmailService
{
    Task SendCustomerInviteAsync(
        string toEmail,
        string customerName,
        string marinaName,
        string temporaryPassword,
        string confirmationLink,
        CancellationToken ct = default);

    Task SendStaffInviteAsync(
        string toEmail,
        string staffName,
        string marinaName,
        string role,
        string temporaryPassword,
        string confirmationLink,
        CancellationToken ct = default);

    Task SendEmailConfirmationAsync(
        string toEmail,
        string recipientName,
        string confirmationLink,
        CancellationToken ct = default);
}
