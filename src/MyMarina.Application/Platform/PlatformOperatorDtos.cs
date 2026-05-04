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
