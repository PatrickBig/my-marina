namespace MyMarina.Domain.Entities;

/// <summary>
/// Junction table linking a User to a specific role within a tenant and optionally marina.
/// A single user can have multiple UserContext records, each representing a different
/// role/tenant/marina combination. At login, the user selects which context to use;
/// the JWT is scoped to that context.
/// </summary>
public class UserContext
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public required Guid TenantId { get; init; }
    public Guid? MarinaId { get; set; }
    public Guid? CustomerAccountId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Role? Role { get; set; }
}
