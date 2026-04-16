using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.WorkOrders;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.WorkOrders;

public class GetWorkOrdersQueryHandler(AppDbContext db)
    : IQueryHandler<GetWorkOrdersQuery, IReadOnlyList<WorkOrderDto>>
{
    public async Task<IReadOnlyList<WorkOrderDto>> HandleAsync(GetWorkOrdersQuery query, CancellationToken ct = default)
    {
        var q = db.WorkOrders.AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(w => w.Status == query.Status.Value);

        if (query.AssignedToUserId.HasValue)
            q = q.Where(w => w.AssignedToUserId == query.AssignedToUserId.Value);

        var items = await q
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkOrderDto(
                w.Id,
                w.MaintenanceRequestId,
                w.MaintenanceRequest != null ? w.MaintenanceRequest.Title : null,
                w.Title,
                w.Description,
                w.AssignedToUserId,
                w.AssignedToUserId != null
                    ? db.Users
                        .Where(u => u.Id == w.AssignedToUserId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault()
                    : null,
                w.Status,
                w.Priority,
                w.ScheduledDate,
                w.CompletedAt,
                w.Notes,
                w.CreatedAt))
            .ToListAsync(ct);

        return items;
    }
}
