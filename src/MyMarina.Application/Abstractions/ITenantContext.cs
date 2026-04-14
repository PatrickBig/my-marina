namespace MyMarina.Application.Abstractions;

/// <summary>
/// Provides the current tenant identity to the application layer.
/// Populated by authentication middleware; never call this from Domain.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsPlatformOperator { get; }
}
