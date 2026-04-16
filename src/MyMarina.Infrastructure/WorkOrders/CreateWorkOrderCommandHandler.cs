using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.WorkOrders;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.WorkOrders;

public class CreateWorkOrderCommandHandler(AppDbContext db, ITenantContext tenantContext)
    : ICommandHandler<CreateWorkOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateWorkOrderCommand command, CancellationToken ct = default)
    {
        // If linking to a maintenance request, verify it belongs to this tenant
        if (command.MaintenanceRequestId.HasValue)
        {
            var exists = await db.MaintenanceRequests
                .AnyAsync(r => r.Id == command.MaintenanceRequestId.Value, ct);
            if (!exists)
                throw new KeyNotFoundException($"Maintenance request {command.MaintenanceRequestId} not found.");
        }

        var workOrder = new WorkOrder
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId,
            MaintenanceRequestId = command.MaintenanceRequestId,
            Title = command.Title,
            Description = command.Description,
            Priority = command.Priority,
            AssignedToUserId = command.AssignedToUserId,
            ScheduledDate = command.ScheduledDate,
            Notes = command.Notes,
        };

        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync(ct);
        return workOrder.Id;
    }
}
