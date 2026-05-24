using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    IBackgroundJobClient jobClient,
    IUserContext userContext,
    IOptions<StorageOptions> storageOptions) : ICommandHandler<UploadMarinaPhotoCommand, MarinaPhotoDto>
{
    public async Task<MarinaPhotoDto> HandleAsync(UploadMarinaPhotoCommand command, CancellationToken ct = default)
    {
        if (!userContext.HasMarinaAccess(command.MarinaId))
            throw new UnauthorizedAccessException("You do not have access to this marina.");

        if (command.FileSizeBytes > storageOptions.Value.S3.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File size exceeds maximum allowed size of {storageOptions.Value.S3.MaxFileSizeBytes / 1_000_000}MB.");

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

        MarinaPhoto photo;
        using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            try
            {
                if (command.Kind is MarinaPhotoKind.Logo or MarinaPhotoKind.Banner)
                {
                    var exists = await db.MarinaPhotos
                        .AnyAsync(p => p.MarinaId == command.MarinaId && p.Kind == command.Kind, ct);
                    if (exists)
                    {
                        await transaction.RollbackAsync(ct);
                        throw new InvalidOperationException(
                            $"A {command.Kind} photo already exists for this marina. Delete it before uploading a new one.");
                    }
                }

                photo = new MarinaPhoto
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
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true)
            {
                await transaction.RollbackAsync(ct);
                throw new InvalidOperationException(
                    $"A {command.Kind} photo already exists for this marina. Delete it before uploading a new one.", ex);
            }
        }

        jobClient.Enqueue<ImageVariantGenerationJob>(j => j.ExecuteAsync(photo.Id));

        return PhotoMappers.ToDto(photo);
    }
}
