using Microsoft.EntityFrameworkCore;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoicing;

public class InvoiceOverdueJob(AppDbContext db)
{
    public async Task CheckOverdueAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Sent && i.DueDate < today)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InvoiceStatus.Overdue));
    }
}
