using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Storage;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
[Route("marinas/{marinaId:guid}/photos")]
public class PhotosController(
    ICommandHandler<CreateUploadTicketCommand, UploadTicket> createTicket,
    ICommandHandler<ConfirmPhotoUploadCommand, MarinaPhotoDto> confirmUpload,
    ICommandHandler<ReorderPhotoCommand> reorderPhoto,
    ICommandHandler<DeletePhotoCommand> deletePhoto,
    IQueryHandler<GetMarinaPhotosQuery, IReadOnlyList<MarinaPhotoDto>> getPhotos,
    IUserContext userContext) : ControllerBase
{
    // POST /api/marinas/{marinaId}/photos/ticket
    [HttpPost("ticket")]
    [ProducesResponseType(typeof(UploadTicket), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTicket(Guid marinaId, [FromBody] CreateUploadTicketRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<MarinaPhotoKind>(request.Kind, ignoreCase: true, out var kind))
            return UnprocessableEntity(Problem("Unknown photo kind.", statusCode: 422));

        try
        {
            var ticket = await createTicket.HandleAsync(new CreateUploadTicketCommand(
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                Kind: kind,
                ContentType: request.ContentType,
                FileSizeBytes: request.FileSizeBytes,
                ImageWidth: request.ImageWidth,
                ImageHeight: request.ImageHeight), ct);
            return Ok(ticket);
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(Problem(ex.Message, statusCode: 422));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Problem(ex.Message, statusCode: 409));
        }
    }

    // POST /api/marinas/{marinaId}/photos/confirm
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(MarinaPhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Confirm(Guid marinaId, [FromBody] ConfirmUploadRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<MarinaPhotoKind>(request.Kind, ignoreCase: true, out var kind))
            return UnprocessableEntity(Problem("Unknown photo kind.", statusCode: 422));

        try
        {
            var photo = await confirmUpload.HandleAsync(new ConfirmPhotoUploadCommand(
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                Key: request.Key,
                Kind: kind,
                Caption: request.Caption,
                Latitude: request.Latitude,
                Longitude: request.Longitude), ct);
            return StatusCode(201, photo);
        }
        catch (StorageObjectNotFoundException)
        {
            return NotFound(Problem("The uploaded file was not found in storage.", statusCode: 404));
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(Problem(ex.Message, statusCode: 422));
        }
    }

    // GET /api/marinas/{marinaId}/photos
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<MarinaPhotoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhotos(Guid marinaId, CancellationToken ct)
    {
        var photos = await getPhotos.HandleAsync(new GetMarinaPhotosQuery(marinaId), ct);
        return Ok(photos);
    }

    // PATCH /api/marinas/{marinaId}/photos/{photoId}/reorder
    [HttpPatch("{photoId:guid}/reorder")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(Guid marinaId, Guid photoId, [FromBody] ReorderPhotoRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<ReorderDirection>(request.Direction, ignoreCase: true, out var direction))
            return BadRequest(Problem("Direction must be 'up' or 'down'.", statusCode: 400));

        try
        {
            await reorderPhoto.HandleAsync(new ReorderPhotoCommand(
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                PhotoId: photoId,
                Direction: direction), ct);
            return Ok();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(Problem(ex.Message, statusCode: 400)); }
    }

    // DELETE /api/marinas/{marinaId}/photos/{photoId}
    [HttpDelete("{photoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid marinaId, Guid photoId, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId) && !userContext.IsPlatformOperator)
            return Forbid();

        try
        {
            await deletePhoto.HandleAsync(new DeletePhotoCommand(
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                PhotoId: photoId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}

// ---------- request records ----------

public sealed record CreateUploadTicketRequest(
    string Kind,
    string ContentType,
    long FileSizeBytes,
    int? ImageWidth = null,
    int? ImageHeight = null
);

public sealed record ConfirmUploadRequest(
    string Key,
    string Kind,
    string? Caption = null,
    decimal? Latitude = null,
    decimal? Longitude = null
);

public sealed record ReorderPhotoRequest(string Direction);
