using Microsoft.EntityFrameworkCore;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that transitions Sent/PartiallyPaid invoices with a
/// past due date to Overdue. Runs cross-tenant so it explicitly bypasses the
/// EF global query filter via IgnoreQueryFilters().
/// </summary>
public class MarkOverdueInvoicesJob(AppDbContext db)
{
    public async Task ExecuteAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueInvoices = await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.DueDate < today &&
                        (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.PartiallyPaid))
            .ToListAsync();

        foreach (var invoice in overdueInvoices)
            invoice.Status = InvoiceStatus.Overdue;

        if (overdueInvoices.Count > 0)
            await db.SaveChangesAsync();
    }
}
