using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Docks;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{nameof(UserRole.MarinaOwner)},{nameof(UserRole.MarinaStaff)}")]
public class DocksController(
    ICommandHandler<CreateDockCommand, Guid> createHandler,
    ICommandHandler<UpdateDockCommand> updateHandler,
    ICommandHandler<DeleteDockCommand> deleteHandler,
    IQueryHandler<GetDocksQuery, IReadOnlyList<DockDto>> getDocksHandler) : ControllerBase
{
    [HttpGet("marinas/{marinaId:guid}/docks")]
    [ProducesResponseType(typeof(IReadOnlyList<DockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid marinaId, CancellationToken ct)
    {
        var docks = await getDocksHandler.HandleAsync(new GetDocksQuery(marinaId), ct);
        return Ok(docks);
    }

    [HttpPost("marinas/{marinaId:guid}/docks")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid marinaId, [FromBody] CreateDockRequest request, CancellationToken ct)
    {
        var command = new CreateDockCommand(marinaId, request.Name, request.Description, request.SortOrder);
        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetAll), new { marinaId }, id);
    }

    [HttpPut("docks/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDockRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdateDockCommand(id, request.Name, request.Description, request.SortOrder);
            await updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("docks/{id:guid}")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await deleteHandler.HandleAsync(new DeleteDockCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record CreateDockRequest(string Name, string? Description, int SortOrder = 0);
public sealed record UpdateDockRequest(string Name, string? Description, int SortOrder);
