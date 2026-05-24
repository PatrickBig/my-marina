using Hangfire;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Photos;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;

namespace MyMarina.Infrastructure.Photos;

public class DeletePhotoCommandHandler(
    AppDbContext db,
    IBackgroundJobClient jobClient,
    IUserContext userContext) : ICommandHandler<DeletePhotoCommand>
{
    public async Task HandleAsync(DeletePhotoCommand command, CancellationToken ct = default)
    {
        if (!userContext.HasMarinaAccess(command.MarinaId))
            throw new UnauthorizedAccessException("You do not have access to this marina.");

        var photo = await db.MarinaPhotos
            .FirstOrDefaultAsync(p => p.Id == command.PhotoId && p.MarinaId == command.MarinaId, ct);

        if (photo is null)
            return;

        var storageKey = photo.StorageKey;
        db.MarinaPhotos.Remove(photo);
        await db.SaveChangesAsync(ct);

        jobClient.Enqueue<StorageCleanupJob>(j => j.ExecuteAsync(storageKey));
    }
}
