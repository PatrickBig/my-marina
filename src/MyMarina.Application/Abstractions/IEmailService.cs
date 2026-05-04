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

    Task SendGhostVesselClaimAsync(
        string toEmail,
        string marinaName,
        string vesselName,
        Guid vesselId,
        CancellationToken ct = default);

    Task SendBillingAccountInviteAsync(
        string toEmail,
        string marinaName,
        string invitedByName,
        Guid memberId,
        CancellationToken ct = default);

    Task SendReservationRequestAsync(
        string toHostEmail,
        string marinaName,
        string boaterName,
        string slipName,
        DateTimeOffset arrivesAt,
        DateTimeOffset departsAt,
        Guid reservationId,
        CancellationToken ct = default);

    Task SendReservationConfirmedAsync(
        string toBoaterEmail,
        string slipName,
        string marinaName,
        DateTimeOffset arrivesAt,
        DateTimeOffset departsAt,
        decimal total,
        Guid reservationId,
        CancellationToken ct = default);

    Task SendReservationDeclinedAsync(
        string toBoaterEmail,
        string slipName,
        string marinaName,
        DateTimeOffset arrivesAt,
        DateTimeOffset departsAt,
        Guid reservationId,
        CancellationToken ct = default);

    Task SendReservationCancelledAsync(
        string toEmail,
        string slipName,
        string marinaName,
        DateTimeOffset arrivesAt,
        DateTimeOffset departsAt,
        Guid reservationId,
        CancellationToken ct = default);

    Task SendInvoiceSentAsync(
        string toEmail,
        string marinaName,
        string billingAccountName,
        string invoiceNumber,
        decimal totalAmount,
        DateOnly dueDate,
        Guid invoiceId,
        CancellationToken ct = default);

    Task SendLeaseApprovedAsync(
        string toEmail,
        string marinaName,
        string slipName,
        string leaseTerm,
        Guid inquiryId,
        CancellationToken ct = default);

    Task SendGenericAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken ct = default);
}
