namespace MyMarina.Domain.Entities;

/// <summary>
/// An authorization permission (capability). Roles can have multiple permissions;
/// evaluation is role-based (no granular permission assignments to users).
/// </summary>
public class Permission
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public string? Description { get; set; }
}
