namespace MyMarina.Application.BillingAccounts;

public sealed record BillingAccountDto(
    Guid Id,
    Guid MarinaId,
    string DisplayName,
    string BillingEmail,
    string? BillingPhone,
    string? BillingAddressStreet,
    string? BillingAddressCity,
    string? BillingAddressState,
    string? BillingAddressZip,
    string? BillingAddressCountry,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public sealed record BillingAccountMemberDto(
    Guid Id,
    Guid BillingAccountId,
    Guid UserId,
    string Role,
    DateTimeOffset InvitedAt,
    DateTimeOffset? AcceptedAt
);
