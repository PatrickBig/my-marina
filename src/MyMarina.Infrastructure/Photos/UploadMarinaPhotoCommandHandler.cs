using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;

namespace MyMarina.Infrastructure.Photos;

public class UploadMarinaPhotoCommandHandler(
    AppDbContext db,
    IStorageProvider storage,
    IBackgroundJobClient jobClient) : ICommandHandler<UploadMarinaPhotoCommand, MarinaPhotoDto>
{
    public async Task<MarinaPhotoDto> HandleAsync(UploadMarinaPhotoCommand command, CancellationToken ct = default)
    {
        // Enforce Logo/Banner uniqueness before writing anything to storage
        if (command.Kind is MarinaPhotoKind.Logo or MarinaPhotoKind.Banner)
        {
            var exists = await db.MarinaPhotos
                .AnyAsync(p => p.MarinaId == command.MarinaId && p.Kind == command.Kind, ct);
            if (exists)
                throw new InvalidOperationException(
                    $"A {command.Kind} photo already exists for this marina. Delete it before uploading a new one.");
        }

        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .Where(m => m.Id == command.MarinaId)
            .Select(m => new { m.TenantId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        var ext = command.ContentType.Split('/').Last().Replace("jpeg", "jpg");
        var photoId = Guid.CreateVersion7();
        var key = $"{marina.TenantId}/{command.MarinaId}/marina/{command.Kind.ToString().ToLower()}/{photoId}.{ext}";

        await storage.PutObjectStreamAsync(key, command.Content, command.ContentType, ct);

        var photo = new MarinaPhoto
        {
            Id = photoId,
            MarinaId = command.MarinaId,
            TenantId = marina.TenantId,
            Kind = command.Kind,
            StorageKey = key,
            FileSizeBytes = command.FileSizeBytes,
            Width = command.Width,
            Height = command.Height,
            Caption = command.Kind == MarinaPhotoKind.Approach ? command.Caption : null,
            Latitude = command.Kind == MarinaPhotoKind.Approach ? command.Latitude : null,
            Longitude = command.Kind == MarinaPhotoKind.Approach ? command.Longitude : null,
            UploadedByUserId = command.RequestingUserId,
        };

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
