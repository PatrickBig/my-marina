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

        foreach (var key in keysToDelete)
        {
            await storage.DeleteAsync(key);
        }

        logger.LogInformation("StorageCleanupJob: deleted {Count} objects for key {Key}", keysToDelete.Length, storageKey);
    }
}
