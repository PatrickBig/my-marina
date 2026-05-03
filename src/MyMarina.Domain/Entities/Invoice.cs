using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Invoice
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public Guid BillingAccountId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? SlipAssignmentId { get; set; }
    public required string InvoiceNumber { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateOnly IssuedDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue => TotalAmount - AmountPaid;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Marina Marina { get; set; } = null!;
    public BillingAccount BillingAccount { get; set; } = null!;
    public ICollection<InvoiceLineItem> LineItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
