using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Announcements;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Controllers;

[ApiController]
[Route("marinas/{marinaId:guid}/announcements")]
[Authorize(Roles = $"{nameof(UserRole.MarinaOwner)},{nameof(UserRole.MarinaStaff)}")]
public class AnnouncementsController(
    ICommandHandler<CreateAnnouncementCommand, Guid> createHandler,
    ICommandHandler<UpdateAnnouncementCommand> updateHandler,
    ICommandHandler<PublishAnnouncementCommand> publishHandler,
    ICommandHandler<UnpublishAnnouncementCommand> unpublishHandler,
    ICommandHandler<DeleteAnnouncementCommand> deleteHandler,
    IQueryHandler<GetAnnouncementsQuery, IReadOnlyList<AnnouncementDto>> getListHandler,
    IQueryHandler<GetAnnouncementQuery, AnnouncementDto?> getOneHandler) : ControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        Guid marinaId,
        [FromQuery] bool includeDrafts = true,
        [FromQuery] bool includeExpired = true,
        CancellationToken ct = default)
    {
        var items = await getListHandler.HandleAsync(
            new GetAnnouncementsQuery(marinaId, includeDrafts, includeExpired), ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid marinaId, Guid id, CancellationToken ct)
    {
        var dto = await getOneHandler.HandleAsync(new GetAnnouncementQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        Guid marinaId, [FromBody] CreateAnnouncementRequest request, CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var id = await createHandler.HandleAsync(
            new CreateAnnouncementCommand(
                marinaId,
                request.Title,
                request.Body,
                request.Publish,
                request.IsPinned,
                request.ExpiresAt,
                userId),
            ct);
        return CreatedAtAction(nameof(GetById), new { marinaId, id }, id);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid marinaId, Guid id, [FromBody] UpdateAnnouncementRequest request, CancellationToken ct)
    {
        try
        {
            await updateHandler.HandleAsync(
                new UpdateAnnouncementCommand(id, request.Title, request.Body, request.IsPinned, request.ExpiresAt), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Publish / Unpublish ───────────────────────────────────────────────────

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid marinaId, Guid id, CancellationToken ct)
    {
        try
        {
            await publishHandler.HandleAsync(new PublishAnnouncementCommand(id), ct);
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

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unpublish(Guid marinaId, Guid id, CancellationToken ct)
    {
        try
        {
            await unpublishHandler.HandleAsync(new UnpublishAnnouncementCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid marinaId, Guid id, CancellationToken ct)
    {
        try
        {
            await deleteHandler.HandleAsync(new DeleteAnnouncementCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public sealed record CreateAnnouncementRequest(
    string Title,
    string Body,
    bool Publish,
    bool IsPinned,
    DateTimeOffset? ExpiresAt);

public sealed record UpdateAnnouncementRequest(
    string Title,
    string Body,
    bool IsPinned,
    DateTimeOffset? ExpiresAt);
