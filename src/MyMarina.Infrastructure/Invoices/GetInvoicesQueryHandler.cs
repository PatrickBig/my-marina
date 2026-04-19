using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Invoices;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Invoices;

public class GetInvoicesQueryHandler(AppDbContext db)
    : IQueryHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> HandleAsync(GetInvoicesQuery query, CancellationToken ct = default)
    {
        var q = db.Invoices.Include(i => i.CustomerAccount).AsQueryable();

        if (query.CustomerAccountId.HasValue)
            q = q.Where(i => i.CustomerAccountId == query.CustomerAccountId.Value);

        if (query.Status.HasValue)
            q = q.Where(i => i.Status == query.Status.Value);

        if (query.IssuedFrom.HasValue)
            q = q.Where(i => i.IssuedDate >= query.IssuedFrom.Value);

        if (query.IssuedTo.HasValue)
            q = q.Where(i => i.IssuedDate <= query.IssuedTo.Value);

        if (query.MarinaId.HasValue)
            q = q.Where(i => i.MarinaId == query.MarinaId.Value);

        var sorted = query.SortBy == "dueDate"
            ? query.SortDescending
                ? q.OrderByDescending(i => i.DueDate).ThenByDescending(i => i.InvoiceNumber)
                : q.OrderBy(i => i.DueDate).ThenBy(i => i.InvoiceNumber)
            : query.SortDescending
                ? q.OrderByDescending(i => i.IssuedDate).ThenByDescending(i => i.InvoiceNumber)
                : q.OrderBy(i => i.IssuedDate).ThenBy(i => i.InvoiceNumber);

        return await sorted
            .Select(i => new InvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.CustomerAccountId,
                i.CustomerAccount.DisplayName,
                i.Status,
                i.IssuedDate,
                i.DueDate,
                i.SubTotal,
                i.TaxAmount,
                i.TotalAmount,
                i.AmountPaid,
                i.TotalAmount - i.AmountPaid,
                i.Notes,
                i.CreatedAt,
                i.MarinaId))
            .ToListAsync(ct);
    }
}
