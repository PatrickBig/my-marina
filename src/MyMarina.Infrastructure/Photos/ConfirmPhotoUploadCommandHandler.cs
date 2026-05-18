using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;
using SixLabors.ImageSharp;

namespace MyMarina.Infrastructure.Photos;

public class ConfirmPhotoUploadCommandHandler(
    AppDbContext db,
    IStorageProvider storage,
    IBackgroundJobClient jobClient) : ICommandHandler<ConfirmPhotoUploadCommand, MarinaPhotoDto>
{
    public async Task<MarinaPhotoDto> HandleAsync(ConfirmPhotoUploadCommand command, CancellationToken ct = default)
    {
        var fileInfo = await storage.ConfirmUploadAsync(command.Key, ct); // throws StorageObjectNotFoundException if missing

        // Read actual dimensions from the uploaded image for server-side validation
        var stream = await storage.GetObjectStreamAsync(command.Key, ct);
        int? width = null, height = null;
        if (stream is not null)
        {
            await using (stream)
            {
                var info = await Image.IdentifyAsync(stream, ct);
                if (info is not null)
                {
                    width = info.Width;
                    height = info.Height;
                }
            }
        }

        if (width.HasValue && height.HasValue)
        {
            var (valid, error) = AspectRatioValidator.Validate(command.Kind, width.Value, height.Value);
            if (!valid) throw new ArgumentException(error);
        }

        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .Where(m => m.Id == command.MarinaId)
            .Select(m => new { m.TenantId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        var photo = new MarinaPhoto
        {
            MarinaId = command.MarinaId,
            TenantId = marina.TenantId,
            Kind = command.Kind,
            StorageKey = command.Key,
            FileSizeBytes = fileInfo.FileSizeBytes,
            Width = width,
            Height = height,
            Caption = command.Kind == MarinaPhotoKind.Approach ? command.Caption : null,
            Latitude = command.Kind == MarinaPhotoKind.Approach ? command.Latitude : null,
            Longitude = command.Kind == MarinaPhotoKind.Approach ? command.Longitude : null,
            UploadedByUserId = Guid.Empty, // set from controller via command extension if needed
        };

        // Assign next sort order for this kind
        var maxSort = await db.MarinaPhotos
            .Where(p => p.MarinaId == command.MarinaId && p.Kind == command.Kind)
            .Select(p => (int?)p.SortOrder)
            .MaxAsync(ct);
        photo.SortOrder = (maxSort ?? -1) + 1;

        db.MarinaPhotos.Add(photo);
        await db.SaveChangesAsync(ct);

        jobClient.Enqueue<ImageVariantGenerationJob>(j => j.ExecuteAsync(photo.Id));

        return PhotoMappers.ToDto(photo);
    }
}
