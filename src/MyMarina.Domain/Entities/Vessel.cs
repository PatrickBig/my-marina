using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Vessel
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    // Null = unclaimed ghost vessel created by marina staff
    public Guid? OwnerUserId { get; set; }

    // Ghost-vessel fields — populated by marina staff before the owner signs up
    public string? ClaimEmail { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }

    public required string Name { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }

    // All dimensions in feet
    public decimal Length { get; set; }
    public decimal Beam { get; set; }
    public decimal Draft { get; set; }

    public BoatType BoatType { get; set; }
    public string? HullColor { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RegistrationState { get; set; }

    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
