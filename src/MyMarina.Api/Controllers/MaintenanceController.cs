using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class MaintenanceController(
    ICommandHandler<SubmitMaintenanceRequestCommand, MaintenanceRequestDto> submitRequest,
    ICommandHandler<UpdateMaintenanceRequestCommand, MaintenanceRequestDto> updateRequest,
    ICommandHandler<CreateWorkOrderCommand, WorkOrderDto> createWorkOrder,
    ICommandHandler<UpdateWorkOrderCommand, WorkOrderDto> updateWorkOrder,
    IQueryHandler<GetMarinaMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>> getMarinaRequests,
    IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestDto?> getRequest,
    IQueryHandler<GetMyMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>> getMyRequests,
    IQueryHandler<GetMarinaWorkOrdersQuery, IReadOnlyList<WorkOrderDto>> getMarinaWorkOrders,
    IQueryHandler<GetWorkOrderQuery, WorkOrderDto?> getWorkOrder)
    : ControllerBase
{
    // ── Boater: submit and view own requests ─────────────────────────────────

    [HttpGet("me/maintenance-requests")]
    public async Task<IActionResult> GetMyRequests(CancellationToken ct)
        => Ok(await getMyRequests.HandleAsync(new GetMyMaintenanceRequestsQuery(), ct));

    [HttpPost("marinas/{marinaId:guid}/maintenance-requests")]
    public async Task<IActionResult> SubmitRequest(
        Guid marinaId, [FromBody] SubmitRequestBody body, CancellationToken ct)
    {
        var dto = await submitRequest.HandleAsync(new SubmitMaintenanceRequestCommand(
            marinaId, body.BillingAccountId, body.VesselId, body.SlipId, body.ReservationId,
            body.Title, body.Description, body.Priority), ct);
        return CreatedAtAction(nameof(GetRequest), new { marinaId, requestId = dto.Id }, dto);
    }

    // ── Marina: manage requests ───────────────────────────────────────────────

    [HttpGet("marinas/{marinaId:guid}/maintenance-requests")]
    public async Task<IActionResult> GetMarinaRequests(
        Guid marinaId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await getMarinaRequests.HandleAsync(
            new GetMarinaMaintenanceRequestsQuery(marinaId, status), ct));

    [HttpGet("marinas/{marinaId:guid}/maintenance-requests/{requestId:guid}")]
    public async Task<IActionResult> GetRequest(
        Guid marinaId, Guid requestId, CancellationToken ct)
    {
        var dto = await getRequest.HandleAsync(new GetMaintenanceRequestQuery(marinaId, requestId), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPatch("marinas/{marinaId:guid}/maintenance-requests/{requestId:guid}")]
    public async Task<IActionResult> UpdateRequest(
        Guid marinaId, Guid requestId, [FromBody] UpdateRequestBody body, CancellationToken ct)
        => Ok(await updateRequest.HandleAsync(
            new UpdateMaintenanceRequestCommand(marinaId, requestId, body.Status, body.Priority), ct));

    // ── Work orders ───────────────────────────────────────────────────────────

    [HttpGet("marinas/{marinaId:guid}/work-orders")]
    public async Task<IActionResult> GetWorkOrders(
        Guid marinaId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await getMarinaWorkOrders.HandleAsync(
            new GetMarinaWorkOrdersQuery(marinaId, status), ct));

    [HttpGet("marinas/{marinaId:guid}/work-orders/{workOrderId:guid}")]
    public async Task<IActionResult> GetWorkOrder(
        Guid marinaId, Guid workOrderId, CancellationToken ct)
    {
        var dto = await getWorkOrder.HandleAsync(new GetWorkOrderQuery(marinaId, workOrderId), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("marinas/{marinaId:guid}/work-orders")]
    public async Task<IActionResult> CreateWorkOrder(
        Guid marinaId, [FromBody] CreateWorkOrderBody body, CancellationToken ct)
    {
        var dto = await createWorkOrder.HandleAsync(new CreateWorkOrderCommand(
            marinaId, body.MaintenanceRequestId, body.Title, body.Description,
            body.AssignedToUserId, body.Priority, body.ScheduledDate), ct);
        return CreatedAtAction(nameof(GetWorkOrder), new { marinaId, workOrderId = dto.Id }, dto);
    }

    [HttpPut("marinas/{marinaId:guid}/work-orders/{workOrderId:guid}")]
    public async Task<IActionResult> UpdateWorkOrder(
        Guid marinaId, Guid workOrderId, [FromBody] UpdateWorkOrderBody body, CancellationToken ct)
        => Ok(await updateWorkOrder.HandleAsync(new UpdateWorkOrderCommand(
            marinaId, workOrderId, body.Title, body.Description,
            body.AssignedToUserId, body.Status, body.Priority,
            body.ScheduledDate, body.Notes), ct));

    // ── Request bodies ────────────────────────────────────────────────────────

    public record SubmitRequestBody(
        Guid? BillingAccountId, Guid? VesselId, Guid? SlipId, Guid? ReservationId,
        string Title, string Description, string Priority = "Medium");

    public record UpdateRequestBody(string Status, string Priority);

    public record CreateWorkOrderBody(
        Guid? MaintenanceRequestId, string Title, string Description,
        Guid? AssignedToUserId, string Priority = "Medium", DateOnly? ScheduledDate = null);

    public record UpdateWorkOrderBody(
        string Title, string Description, Guid? AssignedToUserId,
        string Status, string Priority, DateOnly? ScheduledDate, string? Notes);
}
