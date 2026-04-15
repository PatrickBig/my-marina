namespace MyMarina.Domain.Entities;

/// <summary>
/// A single line item on an invoice. TenantId mirrors Invoice.TenantId
/// so EF Core global query filters are consistent across the relationship.
/// </summary>
public class InvoiceLineItem
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; init; }
    public Guid InvoiceId { get; init; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Optional link for slip-related charges.</summary>
    public Guid? SlipAssignmentId { get; set; }

    public Invoice Invoice { get; init; } = null!;
}
