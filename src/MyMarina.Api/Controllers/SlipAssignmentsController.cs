using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.SlipAssignments;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("slip-assignments")]
[Authorize(Roles = "TenantOwner,MarinaManager,MarinaStaff")]
public class SlipAssignmentsController(
    ICommandHandler<CreateSlipAssignmentCommand, Guid> createHandler,
    ICommandHandler<EndSlipAssignmentCommand> endHandler,
    IQueryHandler<GetSlipAssignmentsQuery, IReadOnlyList<SlipAssignmentDto>> getHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SlipAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? slipId,
        [FromQuery] Guid? customerAccountId,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var assignments = await getHandler.HandleAsync(
            new GetSlipAssignmentsQuery(slipId, customerAccountId, activeOnly), ct);
        return Ok(assignments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSlipAssignmentCommand command, CancellationToken ct)
    {
        try
        {
            var id = await createHandler.HandleAsync(command, ct);
            return CreatedAtAction(nameof(GetAll), id);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> End(Guid id, [FromBody] EndAssignmentRequest request, CancellationToken ct)
    {
        try
        {
            await endHandler.HandleAsync(new EndSlipAssignmentCommand(id, request.EndDate), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record EndAssignmentRequest(DateOnly EndDate);
