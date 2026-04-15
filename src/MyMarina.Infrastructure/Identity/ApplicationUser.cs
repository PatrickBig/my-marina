using Microsoft.AspNetCore.Identity;
using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user extended with marina-specific fields.
/// TenantId is null for platform operators.
/// MarinaId is set for marina-scoped staff; null means access to all
/// marinas within the tenant (corporate operators and customers).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public UserRole Role { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? MarinaId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}
