using MyMarina.Domain.Enums;

namespace MyMarina.Application.Demo;

public sealed record ProvisionDemoTenantCommand(
    string Role,
    SubscriptionTier Tier);

public sealed record ProvisionDemoTenantResult(
    Guid TenantId,
    Guid OperatorUserId,
    Guid CustomerUserId,
    Guid CustomerAccountId);

public sealed record DeleteExpiredDemoTenantsCommand;

public sealed record CreateDemoSessionCommand(
    string Role,
    SubscriptionTier Tier);

public sealed record DemoSessionResult(
    string Token,
    DateTimeOffset ExpiresAt);
