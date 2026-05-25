using Hangfire;
using Microsoft.Extensions.Logging;

namespace MyMarina.Infrastructure.Storage;

public class StorageCleanupJob(IStorageProvider storage, ILogger<StorageCleanupJob> logger)
{
    [Queue("photos")]
    public async Task ExecuteAsync(string storageKey)
    {
        var ext = Path.GetExtension(storageKey);
        var keyWithoutExt = storageKey[..^ext.Length];

        var keysToDelete = new[]
        {
            storageKey,
            $"{keyWithoutExt}_64{ext}",
            $"{keyWithoutExt}_256{ext}",
            $"{keyWithoutExt}_512{ext}",
            $"{keyWithoutExt}_thumb{ext}",
            $"{keyWithoutExt}_medium{ext}",
            $"{keyWithoutExt}_full{ext}",
        };

        int deletedCount = 0;
        foreach (var key in keysToDelete)
        {
            try
            {
                await storage.DeleteAsync(key);
                deletedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "StorageCleanupJob: failed to delete object {Key}", key);
            }
        }

        logger.LogInformation("StorageCleanupJob: deleted {DeletedCount}/{TotalCount} objects for key {Key}",
            deletedCount, keysToDelete.Length, storageKey);
    }
}
