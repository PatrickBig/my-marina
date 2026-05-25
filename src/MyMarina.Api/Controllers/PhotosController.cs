using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Photos;
using MyMarina.Infrastructure.Storage;
using SkiaSharp;

namespace MyMarina.Api.Controllers;

[ApiController]
[Authorize]
[Route("marinas/{marinaId:guid}/photos")]
public class PhotosController(
    ICommandHandler<UploadMarinaPhotoCommand, MarinaPhotoDto> uploadPhoto,
    ICommandHandler<ReorderPhotoCommand> reorderPhoto,
    ICommandHandler<DeletePhotoCommand> deletePhoto,
    IQueryHandler<GetMarinaPhotosQuery, IReadOnlyList<MarinaPhotoDto>> getPhotos,
    IOptions<StorageOptions> storageOptions,
    IUserContext userContext) : ControllerBase
{
    // POST /api/marinas/{marinaId}/photos
    [HttpPost]
    [ProducesResponseType(typeof(MarinaPhotoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status413RequestEntityTooLarge)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Upload(Guid marinaId, [FromForm] UploadPhotoRequest request, CancellationToken ct)
    {
        if (!userContext.HasMarinaAccess(marinaId)) return Forbid();

        if (!Enum.TryParse<MarinaPhotoKind>(request.Kind, ignoreCase: true, out var kind))
            return UnprocessableEntity(Problem("Unknown photo kind.", statusCode: 422));

        var file = request.File;
        var maxBytes = storageOptions.Value.S3.MaxFileSizeBytes;
        if (file.Length > maxBytes)
            return StatusCode(413, Problem($"File exceeds the {maxBytes / 1_048_576} MB limit.", statusCode: 413));

        // Read image dimensions without loading the full file into memory
        int width, height;
        using (var dimStream = file.OpenReadStream())
        {
            using var skData = SKData.Create(dimStream);
            using var codec = SKCodec.Create(skData);
            if (codec is null)
                return UnprocessableEntity(Problem("File could not be decoded as an image.", statusCode: 422));
            width = codec.Info.Width;
            height = codec.Info.Height;
        }

        var (valid, error) = AspectRatioValidator.Validate(kind, width, height);
        if (!valid)
            return UnprocessableEntity(Problem(error, statusCode: 422));

        try
        {
            using var uploadStream = file.OpenReadStream();
            var photo = await uploadPhoto.HandleAsync(new UploadMarinaPhotoCommand(
                MarinaId: marinaId,
                RequestingUserId: userContext.UserId!.Value,
                Kind: kind,
                Content: uploadStream,
                ContentType: file.ContentType,
                FileSizeBytes: file.Length,
                Width: width,
                Height: height,
                Caption: request.Caption,
                Latitude: request.Latitude,
                Longitude: request.Longitude), ct);
            return StatusCode(201, photo);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Problem(ex.Message, statusCode: 409));
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

public sealed record UploadPhotoRequest(
    IFormFile File,
    string Kind,
    string? Caption = null,
    decimal? Latitude = null,
    decimal? Longitude = null
);

public sealed record ReorderPhotoRequest(string Direction);
