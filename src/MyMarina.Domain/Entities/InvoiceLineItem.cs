namespace MyMarina.Domain.Entities;

public class InvoiceLineItem
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid InvoiceId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? SlipAssignmentId { get; set; }
    public Guid? ReservationId { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
}
