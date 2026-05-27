using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetBillingSummaryQueryHandler(AppDbContext db)
    : IQueryHandler<GetBillingSummaryQuery, BillingSummaryDto>
{
    public async Task<BillingSummaryDto> HandleAsync(GetBillingSummaryQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        // Minimal projection — pull only status + computed balance for each invoice
        var invoiceData = await db.Invoices
            .Where(i => i.MarinaId == query.MarinaId)
            .Select(i => new { i.Status, BalanceDue = i.TotalAmount - i.AmountPaid })
            .ToListAsync(ct);

        var outstanding = invoiceData
            .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue)
            .Sum(i => i.BalanceDue);

        var overdueItems = invoiceData.Where(i => i.Status == InvoiceStatus.Overdue).ToList();

        var collectedThisMonth = await db.Payments
            .Where(p => p.Invoice.MarinaId == query.MarinaId &&
                        p.PaidOn >= startOfMonth &&
                        p.PaidOn < endOfMonth)
            .SumAsync(p => p.Amount, ct);

        return new BillingSummaryDto(
            TotalOutstanding:  outstanding,
            OverdueCount:      overdueItems.Count,
            TotalOverdue:      overdueItems.Sum(i => i.BalanceDue),
            CollectedThisMonth: collectedThisMonth,
            DraftCount:        invoiceData.Count(i => i.Status == InvoiceStatus.Draft),
            SentCount:         invoiceData.Count(i => i.Status == InvoiceStatus.Sent));
    }
}
