namespace MyMarina.Domain.Entities;

/// <summary>
/// A non-billable cost incurred by the marina (e.g., maintenance labor, supplies, fuel).
/// Unlike Invoice charges (passed to customers), operating expenses are the marina's own costs.
/// Scoped to a specific marina for cost tracking and profitability analysis.
/// </summary>
public class OperatingExpense
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid TenantId { get; init; }
    public required Guid MarinaId { get; init; }
    public required string Category { get; set; }
    public required string Description { get; set; }
    public required decimal Amount { get; set; }
    public required DateOnly IncurredDate { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public required Guid RecordedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
