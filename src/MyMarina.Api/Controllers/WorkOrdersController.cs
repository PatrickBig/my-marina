using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.WorkOrders;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("work-orders")]
[Authorize(Roles = "TenantOwner,MarinaManager,MarinaStaff")]
public class WorkOrdersController(
    ICommandHandler<CreateWorkOrderCommand, Guid> createHandler,
    ICommandHandler<UpdateWorkOrderCommand> updateHandler,
    ICommandHandler<CompleteWorkOrderCommand> completeHandler,
    IQueryHandler<GetWorkOrdersQuery, IReadOnlyList<WorkOrderDto>> getListHandler,
    IQueryHandler<GetWorkOrderQuery, WorkOrderDto?> getOneHandler) : ControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] WorkOrderStatus? status,
        [FromQuery] Guid? assignedToUserId,
        CancellationToken ct = default)
    {
        var items = await getListHandler.HandleAsync(new GetWorkOrdersQuery(status, assignedToUserId), ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await getOneHandler.HandleAsync(new GetWorkOrderQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request, CancellationToken ct)
    {
        try
        {
            var id = await createHandler.HandleAsync(
                new CreateWorkOrderCommand(
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.MaintenanceRequestId,
                    request.AssignedToUserId,
                    request.ScheduledDate,
                    request.Notes),
                ct);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkOrderRequest request, CancellationToken ct)
    {
        try
        {
            await updateHandler.HandleAsync(
                new UpdateWorkOrderCommand(
                    id,
                    request.Title,
                    request.Description,
                    request.Priority,
                    request.Status,
                    request.AssignedToUserId,
                    request.ScheduledDate,
                    request.Notes),
                ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteWorkOrderRequest request, CancellationToken ct)
    {
        try
        {
            await completeHandler.HandleAsync(new CompleteWorkOrderCommand(id, request.Notes), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record CreateWorkOrderRequest(
    string Title,
    string Description,
    Priority Priority,
    Guid? MaintenanceRequestId,
    Guid? AssignedToUserId,
    DateOnly? ScheduledDate,
    string? Notes);

public sealed record UpdateWorkOrderRequest(
    string Title,
    string Description,
    Priority Priority,
    WorkOrderStatus Status,
    Guid? AssignedToUserId,
    DateOnly? ScheduledDate,
    string? Notes);

public sealed record CompleteWorkOrderRequest(string? Notes);
