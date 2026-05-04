namespace MyMarina.Domain.Entities;

public class OwnerAbsence
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid SlipAssignmentId { get; set; }
    public Guid SlipId { get; set; } // denormalized for efficient marina-side queries

    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public SlipAssignment Assignment { get; set; } = null!;
}
