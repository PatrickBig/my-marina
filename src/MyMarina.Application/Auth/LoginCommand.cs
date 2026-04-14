using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResult(
    string Token,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? TenantId,
    Guid? MarinaId);

public interface ILoginCommandHandler : ICommandHandler<LoginCommand, LoginResult>;
