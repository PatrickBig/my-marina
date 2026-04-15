using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("marinas")]
[Authorize(Roles = $"{nameof(UserRole.MarinaOwner)},{nameof(UserRole.MarinaStaff)}")]
public class MarinasController(
    ICommandHandler<CreateMarinaCommand, Guid> createHandler,
    ICommandHandler<UpdateMarinaCommand> updateHandler,
    IQueryHandler<GetMarinasQuery, IReadOnlyList<MarinaDto>> getMarinasHandler,
    IQueryHandler<GetMarinaQuery, MarinaDto?> getMarinaHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MarinaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var marinas = await getMarinasHandler.HandleAsync(new GetMarinasQuery(), ct);
        return Ok(marinas);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MarinaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var marina = await getMarinaHandler.HandleAsync(new GetMarinaQuery(id), ct);
        return marina is null ? NotFound() : Ok(marina);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMarinaCommand command, CancellationToken ct)
    {
        var id = await createHandler.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.MarinaOwner))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMarinaRequest request, CancellationToken ct)
    {
        try
        {
            var command = new UpdateMarinaCommand(
                id, request.Name, request.Address, request.PhoneNumber,
                request.Email, request.TimeZoneId, request.Website, request.Description);
            await updateHandler.HandleAsync(command, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

public sealed record UpdateMarinaRequest(
    string Name,
    AddressDto Address,
    string PhoneNumber,
    string Email,
    string TimeZoneId,
    string? Website,
    string? Description);
