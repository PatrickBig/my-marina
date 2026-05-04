using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class GetLeaseInquiriesQueryHandler(AppDbContext db, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetLeaseInquiriesQuery, IReadOnlyList<LeaseInquiryDto>>
{
    public async Task<IReadOnlyList<LeaseInquiryDto>> HandleAsync(GetLeaseInquiriesQuery query, CancellationToken ct = default)
    {
        var q = db.SlipLeaseInquiries
            .Include(i => i.Slip)
            .Where(i => i.MarinaId == query.MarinaId);

        if (query.SlipId.HasValue)
            q = q.Where(i => i.SlipId == query.SlipId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeaseInquiryStatus>(query.Status, ignoreCase: true, out var statusFilter))
            q = q.Where(i => i.Status == statusFilter);

        var inquiries = await q.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        var tasks = inquiries.Select(i => LeaseInquiryMappers.ToDtoAsync(i, userManager, db, ct));
        return await Task.WhenAll(tasks);
    }
}

public class GetMyLeaseInquiriesQueryHandler(AppDbContext db, UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetMyLeaseInquiriesQuery, IReadOnlyList<LeaseInquiryDto>>
{
    public async Task<IReadOnlyList<LeaseInquiryDto>> HandleAsync(GetMyLeaseInquiriesQuery query, CancellationToken ct = default)
    {
        var inquiries = await db.SlipLeaseInquiries
            .Include(i => i.Slip)
            .Where(i => i.RequestingUserId == query.RequestingUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        var tasks = inquiries.Select(i => LeaseInquiryMappers.ToDtoAsync(i, userManager, db, ct));
        return await Task.WhenAll(tasks);
    }
}
