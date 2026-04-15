using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Tenants;

public sealed record GetTenantsQuery;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    SubscriptionTier SubscriptionTier,
    DateTimeOffset CreatedAt);

public interface IGetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>;
