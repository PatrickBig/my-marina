using MyMarina.Domain.Common;

namespace MyMarina.Domain.Entities;

/// <summary>
/// A named section of a marina containing slips (e.g., "Dock A", "North Dock").
/// </summary>
public class Dock : TenantEntity
{
    public Guid MarinaId { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    public Marina Marina { get; init; } = null!;
    public ICollection<Slip> Slips { get; init; } = [];
}
