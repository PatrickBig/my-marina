namespace MyMarina.Application.Identity;

public sealed record UpdateProfileCommand(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool? MarketingOptIn
);
