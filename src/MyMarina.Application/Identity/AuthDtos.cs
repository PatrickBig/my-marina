namespace MyMarina.Application.Identity;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserProfileDto User
);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfilePhotoUrl,
    bool MarketingOptIn,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);
