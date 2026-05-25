using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Platform;

public record GetTenantsQuery(string? Search = null, int Page = 1, int PageSize = 25);
public record SearchUsersQuery(string? Q = null, int Page = 1, int PageSize = 25);
public record GetUserProfileQuery(Guid UserId);
public record GetAuditLogQuery(Guid? TenantId = null, int Page = 1, int PageSize = 50);
public record GetListingModerationQueueQuery(int Page = 1, int PageSize = 50);
