namespace MyMarina.Domain.Entities;

/// <summary>
/// An authorization role. Defines what actions are available to a user in a given context.
/// </summary>
public class Role
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<UserContext> UserContexts { get; init; } = [];
}
