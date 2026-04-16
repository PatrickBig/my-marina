using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Tenants;

public sealed record GetTenantsQuery;
public sealed record GetTenantQuery(Guid TenantId);

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    SubscriptionTier SubscriptionTier,
    DateTimeOffset CreatedAt);

public sealed record TenantMarinaDto(Guid Id, string Name, DateTimeOffset CreatedAt);

public sealed record TenantOwnerDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive);

public sealed record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    SubscriptionTier SubscriptionTier,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TenantMarinaDto> Marinas,
    TenantOwnerDto? Owner);

public interface IGetTenantsQueryHandler : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>;
public interface IGetTenantQueryHandler : IQueryHandler<GetTenantQuery, TenantDetailDto?>;
