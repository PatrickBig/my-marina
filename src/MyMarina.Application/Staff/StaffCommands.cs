using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Staff;

public sealed record InviteStaffCommand(
    Guid MarinaId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role);

/// <summary>
/// Returns the created user ID and a temporary password.
/// In production this would trigger an email invitation; for MVP we return
/// the credentials directly so the marina owner can share them out-of-band.
/// </summary>
public sealed record InviteStaffResult(Guid UserId, string TemporaryPassword);

public interface IInviteStaffCommandHandler : ICommandHandler<InviteStaffCommand, InviteStaffResult>;
