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

    public Task SendGhostVesselClaimAsync(string toEmail, string marinaName, string vesselName,
        Guid vesselId, CancellationToken ct = default)
    {
        var subject = $"Your boat has been added at {marinaName}";
        var body = EmailTemplates.GhostVesselClaim(toEmail, marinaName, vesselName, vesselId);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, toEmail, subject, body), ct);
    }

    public Task SendBillingAccountInviteAsync(string toEmail, string marinaName, string invitedByName,
        Guid memberId, CancellationToken ct = default)
    {
        var subject = $"You've been added to a billing account at {marinaName}";
        var body = EmailTemplates.BillingAccountInvite(toEmail, marinaName, invitedByName, memberId);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, toEmail, subject, body), ct);
    }

    public Task SendReservationRequestAsync(string toHostEmail, string marinaName, string boaterName,
        string slipName, DateTimeOffset arrivesAt, DateTimeOffset departsAt,
        Guid reservationId, CancellationToken ct = default)
    {
        var subject = $"New booking request for {slipName} at {marinaName}";
        var body = EmailTemplates.ReservationRequest(toHostEmail, marinaName, boaterName, slipName, arrivesAt, departsAt, reservationId);
        return messageBus.PublishAsync(new SendEmailMessage(toHostEmail, toHostEmail, subject, body), ct);
    }

    public Task SendReservationConfirmedAsync(string toBoaterEmail, string slipName, string marinaName,
        DateTimeOffset arrivesAt, DateTimeOffset departsAt, decimal total,
        Guid reservationId, CancellationToken ct = default)
    {
        var subject = $"Your booking at {marinaName} is confirmed";
        var body = EmailTemplates.ReservationConfirmed(toBoaterEmail, slipName, marinaName, arrivesAt, departsAt, total, reservationId);
        return messageBus.PublishAsync(new SendEmailMessage(toBoaterEmail, toBoaterEmail, subject, body), ct);
    }

    public Task SendReservationDeclinedAsync(string toBoaterEmail, string slipName, string marinaName,
        DateTimeOffset arrivesAt, DateTimeOffset departsAt,
        Guid reservationId, CancellationToken ct = default)
    {
        var subject = $"Your booking request at {marinaName} was declined";
        var body = EmailTemplates.ReservationDeclined(toBoaterEmail, slipName, marinaName, arrivesAt, departsAt, reservationId);
        return messageBus.PublishAsync(new SendEmailMessage(toBoaterEmail, toBoaterEmail, subject, body), ct);
    }

    public Task SendReservationCancelledAsync(string toEmail, string slipName, string marinaName,
        DateTimeOffset arrivesAt, DateTimeOffset departsAt,
        Guid reservationId, CancellationToken ct = default)
    {
        var subject = $"Booking cancelled — {slipName} at {marinaName}";
        var body = EmailTemplates.ReservationCancelled(toEmail, slipName, marinaName, arrivesAt, departsAt, reservationId);
        return messageBus.PublishAsync(new SendEmailMessage(toEmail, toEmail, subject, body), ct);
    }
}
