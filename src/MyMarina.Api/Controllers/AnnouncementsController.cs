using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
public class AnnouncementsController(
    ICommandHandler<CreateAnnouncementCommand, AnnouncementDto> create,
    ICommandHandler<UpdateAnnouncementCommand, AnnouncementDto> update,
    ICommandHandler<PublishAnnouncementCommand, AnnouncementDto> publish,
    ICommandHandler<DeleteAnnouncementCommand> delete,
    IQueryHandler<GetMarinaAnnouncementsQuery, IReadOnlyList<AnnouncementDto>> getMarina,
    IQueryHandler<GetMyAnnouncementsQuery, IReadOnlyList<AnnouncementDto>> getMy)
    : ControllerBase
{
    // ── Boater: view published announcements ─────────────────────────────────

    [HttpGet("me/announcements")]
    public async Task<IActionResult> GetMyAnnouncements(CancellationToken ct)
        => Ok(await getMy.HandleAsync(new GetMyAnnouncementsQuery(), ct));

    // ── Marina: manage announcements ─────────────────────────────────────────

    [HttpGet("marinas/{marinaId:guid}/announcements")]
    public async Task<IActionResult> GetMarinaAnnouncements(Guid marinaId, CancellationToken ct)
        => Ok(await getMarina.HandleAsync(new GetMarinaAnnouncementsQuery(marinaId), ct));

    [HttpPost("marinas/{marinaId:guid}/announcements")]
    public async Task<IActionResult> Create(
        Guid marinaId, [FromBody] AnnouncementBody body, CancellationToken ct)
    {
        var dto = await create.HandleAsync(new CreateAnnouncementCommand(
            marinaId, body.Title, body.Body, body.Audience, body.IsPinned, body.ExpiresAt), ct);
        return CreatedAtAction(nameof(GetMarinaAnnouncements), new { marinaId }, dto);
    }

    [HttpPut("marinas/{marinaId:guid}/announcements/{announcementId:guid}")]
    public async Task<IActionResult> Update(
        Guid marinaId, Guid announcementId, [FromBody] AnnouncementBody body, CancellationToken ct)
        => Ok(await update.HandleAsync(new UpdateAnnouncementCommand(
            marinaId, announcementId, body.Title, body.Body,
            body.Audience, body.IsPinned, body.ExpiresAt), ct));

    [HttpPost("marinas/{marinaId:guid}/announcements/{announcementId:guid}/publish")]
    public async Task<IActionResult> Publish(
        Guid marinaId, Guid announcementId, CancellationToken ct)
        => Ok(await publish.HandleAsync(new PublishAnnouncementCommand(marinaId, announcementId), ct));

    [HttpDelete("marinas/{marinaId:guid}/announcements/{announcementId:guid}")]
    public async Task<IActionResult> Delete(
        Guid marinaId, Guid announcementId, CancellationToken ct)
    {
        await delete.HandleAsync(new DeleteAnnouncementCommand(marinaId, announcementId), ct);
        return NoContent();
    }

    public record AnnouncementBody(
        string Title, string Body, string Audience = "Both",
        bool IsPinned = false, DateTimeOffset? ExpiresAt = null);
}
