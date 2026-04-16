using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalInvoicesQueryHandler(
    AppDbContext db,
    ICustomerContext customerContext) : IQueryHandler<GetPortalInvoicesQuery, IReadOnlyList<PortalInvoiceDto>>
{
    public async Task<IReadOnlyList<PortalInvoiceDto>> HandleAsync(GetPortalInvoicesQuery query, CancellationToken ct = default)
    {
        return await db.Invoices
            .Where(i => i.CustomerAccountId == customerContext.CustomerAccountId)
            .OrderByDescending(i => i.IssuedDate)
            .Select(i => new PortalInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.Status,
                i.IssuedDate,
                i.DueDate,
                i.TotalAmount,
                i.AmountPaid,
                i.BalanceDue,
                i.Notes,
                i.CreatedAt))
            .ToListAsync(ct);
    }
}
