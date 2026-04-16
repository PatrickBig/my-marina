using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.WorkOrders;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.WorkOrders;

public class UpdateWorkOrderCommandHandler(AppDbContext db) : ICommandHandler<UpdateWorkOrderCommand>
{
    public async Task HandleAsync(UpdateWorkOrderCommand command, CancellationToken ct = default)
    {
        var workOrder = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == command.WorkOrderId, ct)
            ?? throw new KeyNotFoundException($"Work order {command.WorkOrderId} not found.");

        workOrder.Title = command.Title;
        workOrder.Description = command.Description;
        workOrder.Priority = command.Priority;
        workOrder.Status = command.Status;
        workOrder.AssignedToUserId = command.AssignedToUserId;
        workOrder.ScheduledDate = command.ScheduledDate;
        workOrder.Notes = command.Notes;

        await db.SaveChangesAsync(ct);
    }
}
