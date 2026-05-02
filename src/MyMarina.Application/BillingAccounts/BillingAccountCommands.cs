using MyMarina.Domain.Enums;

namespace MyMarina.Application.BillingAccounts;

public sealed record CreateBillingAccountCommand(
    Guid MarinaId,
    Guid RequestingUserId,
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
    string? Notes
);

public sealed record UpdateBillingAccountCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId,
    string? DisplayName,
    string? BillingEmail,
    string? BillingPhone,
    string? BillingAddressStreet,
    string? BillingAddressCity,
    string? BillingAddressState,
    string? BillingAddressZip,
    string? BillingAddressCountry,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    bool? IsActive
);

public sealed record InviteBillingAccountMemberCommand(
    Guid BillingAccountId,
    Guid MarinaId,
    Guid RequestingUserId,
    string Email,
    BillingAccountRole Role
);

public sealed record AcceptBillingAccountMemberInviteCommand(
    Guid BillingAccountMemberId,
    Guid UserId
);

public sealed record RemoveBillingAccountMemberCommand(
    Guid BillingAccountMemberId,
    Guid BillingAccountId,
    Guid MarinaId,
    Guid RequestingUserId
);
