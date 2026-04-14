using MyMarina.Domain.Enums;

namespace MyMarina.Application.Abstractions;

/// <summary>
/// Generates signed JWT tokens for authenticated users.
/// Implemented in Infrastructure to keep token signing concerns out of Application.
/// </summary>
public interface IJwtTokenService
{
    string GenerateToken(UserTokenInfo user);
}

/// <summary>
/// Caller-assembled user identity snapshot used by IJwtTokenService.
/// Decouples the token service from ApplicationUser (an Infrastructure type).
/// </summary>
public sealed record UserTokenInfo(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? TenantId,
    Guid? MarinaId);
