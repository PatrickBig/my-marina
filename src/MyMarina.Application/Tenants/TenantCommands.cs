using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Tenants;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    string OwnerEmail,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerPassword,
    SubscriptionTier SubscriptionTier = SubscriptionTier.Free);

public sealed record CreateTenantResult(Guid TenantId, Guid OwnerId);

public sealed record UpdateTenantCommand(
    Guid TenantId,
    string Name,
    bool IsActive,
    SubscriptionTier SubscriptionTier);

public interface ICreateTenantCommandHandler : ICommandHandler<CreateTenantCommand, CreateTenantResult>;
public interface IUpdateTenantCommandHandler : ICommandHandler<UpdateTenantCommand>;
