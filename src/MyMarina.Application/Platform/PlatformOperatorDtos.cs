namespace MyMarina.Application.Platform;

public record TenantSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string SubscriptionTier,
    bool IsActive,
    bool IsDemo,
    DateTimeOffset? SuspendedAt,
    DateTimeOffset CreatedAt,
    int MarinaCount);

public record CreateTenantResponse(Guid Id, string Name, string Slug);

public record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    bool IsPlatformOperator);

public record AuditLogEntryDto(
    Guid Id,
    Guid? TenantId,
    Guid? ActorUserId,
    string ActorName,
    string Action,
    string? TargetType,
    string? TargetId,
    string? Details,
    DateTimeOffset OccurredAt);

public record ListingModerationDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    Guid MarinaId,
    string MarinaName,
    Guid TenantId,
    string TenantName,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal BasePricePerNight,
    string ListedByKind,
    DateTimeOffset CreatedAt);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record UserProfileDto(
    UserSummaryDto User,
    List<VesselSummaryDto> Vessels,
    List<ReservationSummaryDto> Reservations,
    List<MembershipSummaryDto> Memberships,
    List<AuditLogEntryDto> RecentActivity);

public record VesselSummaryDto(
    Guid Id,
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    string BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState,
    bool IsArchived,
    DateTimeOffset CreatedAt);

public record ReservationSummaryDto(
    Guid Id,
    Guid VesselId,
    string VesselName,
    Guid SlipId,
    string SlipName,
    Guid MarinaId,
    string MarinaName,
    DateTimeOffset ArrivesAt,
    DateTimeOffset DepartsAt,
    string Status,
    decimal BasePrice,
    decimal Fees,
    decimal Taxes,
    decimal Total,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CancelledAt);

public record MembershipSummaryDto(
    Guid Id,
    Guid UserId,
    Guid TenantId,
    Guid? MarinaId,
    string Scope,
    string Role,
    DateTimeOffset InvitedAt,
    DateTimeOffset? AcceptedAt,
    bool IsPending);
