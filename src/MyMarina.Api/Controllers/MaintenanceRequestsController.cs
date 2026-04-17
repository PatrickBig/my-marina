using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("maintenance-requests")]
[Authorize(Roles = "TenantOwner,MarinaManager,MarinaStaff")]
public class MaintenanceRequestsController(
    ICommandHandler<UpdateMaintenanceStatusCommand> updateStatusHandler,
    IQueryHandler<GetMaintenanceRequestsQuery, IReadOnlyList<MaintenanceRequestDto>> getListHandler,
    IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestDto?> getOneHandler) : ControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] MaintenanceStatus? status,
        [FromQuery] Priority? priority,
        CancellationToken ct = default)
    {
        var items = await getListHandler.HandleAsync(new GetMaintenanceRequestsQuery(status, priority), ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MaintenanceRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await getOneHandler.HandleAsync(new GetMaintenanceRequestQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    // ── Status update ─────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] UpdateMaintenanceStatusRequest request, CancellationToken ct)
    {
        try
        {
            await updateStatusHandler.HandleAsync(
                new UpdateMaintenanceStatusCommand(id, request.Status), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record UpdateMaintenanceStatusRequest(MaintenanceStatus Status);
