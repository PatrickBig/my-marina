using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Maintenance;

public sealed record UpdateMaintenanceStatusCommand(
    Guid MaintenanceRequestId,
    MaintenanceStatus Status);

public interface IUpdateMaintenanceStatusCommandHandler : ICommandHandler<UpdateMaintenanceStatusCommand>;
