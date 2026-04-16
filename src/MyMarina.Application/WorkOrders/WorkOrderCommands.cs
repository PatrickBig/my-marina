using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.WorkOrders;

public sealed record CreateWorkOrderCommand(
    string Title,
    string Description,
    Priority Priority,
    Guid? MaintenanceRequestId,
    Guid? AssignedToUserId,
    DateOnly? ScheduledDate,
    string? Notes);

public sealed record UpdateWorkOrderCommand(
    Guid WorkOrderId,
    string Title,
    string Description,
    Priority Priority,
    WorkOrderStatus Status,
    Guid? AssignedToUserId,
    DateOnly? ScheduledDate,
    string? Notes);

public sealed record CompleteWorkOrderCommand(
    Guid WorkOrderId,
    string? Notes);

public interface ICreateWorkOrderCommandHandler : ICommandHandler<CreateWorkOrderCommand, Guid>;
public interface IUpdateWorkOrderCommandHandler : ICommandHandler<UpdateWorkOrderCommand>;
public interface ICompleteWorkOrderCommandHandler : ICommandHandler<CompleteWorkOrderCommand>;
