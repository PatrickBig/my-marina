using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.WorkOrders;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.WorkOrders;

public class CompleteWorkOrderCommandHandler(AppDbContext db) : ICommandHandler<CompleteWorkOrderCommand>
{
    public async Task HandleAsync(CompleteWorkOrderCommand command, CancellationToken ct = default)
    {
        var workOrder = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == command.WorkOrderId, ct)
            ?? throw new KeyNotFoundException($"Work order {command.WorkOrderId} not found.");

        if (workOrder.Status == WorkOrderStatus.Completed)
            throw new InvalidOperationException("Work order is already completed.");

        workOrder.Status = WorkOrderStatus.Completed;
        workOrder.CompletedAt = DateTimeOffset.UtcNow;
        if (command.Notes is not null)
            workOrder.Notes = command.Notes;

        await db.SaveChangesAsync(ct);
    }
}
