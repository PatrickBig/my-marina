using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Leases;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Leases;

public class GetLeaseInquiriesQueryHandler(AppDbContext db)
    : IQueryHandler<GetLeaseInquiriesQuery, IReadOnlyList<LeaseInquiryDto>>
{
    public async Task<IReadOnlyList<LeaseInquiryDto>> HandleAsync(GetLeaseInquiriesQuery query, CancellationToken ct = default)
    {
        var q = db.SlipLeaseInquiries
            .Where(i => i.MarinaId == query.MarinaId);

        if (query.SlipId.HasValue)
            q = q.Where(i => i.SlipId == query.SlipId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<LeaseInquiryStatus>(query.Status, ignoreCase: true, out var statusFilter))
            q = q.Where(i => i.Status == statusFilter);

        return await LeaseInquiryMappers.QueryAsync(q.OrderByDescending(i => i.CreatedAt), db, ct);
    }
}

public class GetMyLeaseInquiriesQueryHandler(AppDbContext db)
    : IQueryHandler<GetMyLeaseInquiriesQuery, IReadOnlyList<LeaseInquiryDto>>
{
    public async Task<IReadOnlyList<LeaseInquiryDto>> HandleAsync(GetMyLeaseInquiriesQuery query, CancellationToken ct = default)
    {
        return await LeaseInquiryMappers.QueryAsync(
            db.SlipLeaseInquiries
                .Where(i => i.RequestingUserId == query.RequestingUserId)
                .OrderByDescending(i => i.CreatedAt),
            db,
            ct);
    }
}
