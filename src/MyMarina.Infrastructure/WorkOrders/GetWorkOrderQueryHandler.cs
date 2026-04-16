using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.WorkOrders;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.WorkOrders;

public class GetWorkOrderQueryHandler(AppDbContext db)
    : IQueryHandler<GetWorkOrderQuery, WorkOrderDto?>
{
    public async Task<WorkOrderDto?> HandleAsync(GetWorkOrderQuery query, CancellationToken ct = default)
    {
        return await db.WorkOrders
            .Where(w => w.Id == query.WorkOrderId)
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
            .FirstOrDefaultAsync(ct);
    }
}
