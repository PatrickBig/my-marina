using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Storage;

public class OrphanPhotoCleanupJob(
    AppDbContext db,
    IBackgroundJobClient jobClient,
    ILogger<OrphanPhotoCleanupJob> logger)
{
    [Queue("photos")]
    public async Task ExecuteAsync()
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-5);
        var orphans = await db.MarinaPhotos
            .Where(p => p.UrlFull == null && p.UploadedAt < cutoff)
            .ToListAsync();

        foreach (var photo in orphans)
        {
            logger.LogInformation("OrphanPhotoCleanupJob: deleting orphaned photo {PhotoId} {StorageKey} uploaded at {UploadedAt}",
                photo.Id, photo.StorageKey, photo.UploadedAt);
            jobClient.Enqueue<StorageCleanupJob>(j => j.ExecuteAsync(photo.StorageKey));
            db.MarinaPhotos.Remove(photo);
        }

        if (orphans.Count > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("OrphanPhotoCleanupJob: removed {Count} orphaned photo records", orphans.Count);
        }
    }
}
