using Hangfire;
using Microsoft.Extensions.Logging;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace MyMarina.Infrastructure.Storage;

public class ImageVariantGenerationJob(
    AppDbContext db,
    IStorageProvider storage,
    ILogger<ImageVariantGenerationJob> logger)
{
    private static readonly (string Suffix, int Size)[] SquareSizes =
        [("_64", 64), ("_256", 256), ("_512", 512)];

    private static readonly (string Suffix, int Width)[] LandscapeWidths =
        [("_thumb", 400), ("_medium", 800), ("_full", 2000)];

    [Queue("photos")]
    public async Task ExecuteAsync(Guid photoId)
    {
        var photo = await db.MarinaPhotos.FindAsync(photoId);
        if (photo is null)
        {
            logger.LogWarning("ImageVariantGenerationJob: photo {PhotoId} not found", photoId);
            return;
        }

        var originalKey = photo.StorageKey;
        var ext = Path.GetExtension(originalKey).TrimStart('.').ToLower();
        var keyWithoutExt = originalKey[..^(ext.Length + 1)];
        IImageFormat format = ext is "jpg" or "jpeg" ? JpegFormat.Instance : PngFormat.Instance;
        var contentType = ext is "jpg" or "jpeg" ? "image/jpeg" : "image/png";

        var originalStream = await storage.GetObjectStreamAsync(originalKey);
        if (originalStream is null)
        {
            logger.LogWarning("ImageVariantGenerationJob: original not found for photo {PhotoId} key {Key}", photoId, originalKey);
            return;
        }

        await using (originalStream)
        {
            using var image = await Image.LoadAsync(originalStream);
            photo.Width = image.Width;
            photo.Height = image.Height;

            if (photo.Kind == MarinaPhotoKind.Logo)
            {
                foreach (var (suffix, size) in SquareSizes)
                {
                    var variantKey = $"{keyWithoutExt}{suffix}.{ext}";
                    using var variant = image.Clone(ctx => ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(size, size),
                        Mode = ResizeMode.Crop,
                        Position = AnchorPositionMode.Center,
                    }));
                    await SaveVariantAsync(variant, variantKey, contentType, format);
                    SetVariantUrl(photo, suffix, storage.GetPublicUrl(variantKey));
                }
            }
            else
            {
                foreach (var (suffix, width) in LandscapeWidths)
                {
                    var variantKey = $"{keyWithoutExt}{suffix}.{ext}";
                    using var variant = image.Clone(ctx => ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(width, 0),
                        Mode = ResizeMode.Max,
                    }));
                    await SaveVariantAsync(variant, variantKey, contentType, format);
                    SetVariantUrl(photo, suffix, storage.GetPublicUrl(variantKey));
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("ImageVariantGenerationJob: variants generated for photo {PhotoId}", photoId);
    }

    private async Task SaveVariantAsync(Image variant, string key, string contentType, IImageFormat format)
    {
        using var ms = new MemoryStream();
        await variant.SaveAsync(ms, format);
        ms.Position = 0;
        await storage.PutObjectStreamAsync(key, ms, contentType);
    }

    private static void SetVariantUrl(Domain.Entities.MarinaPhoto photo, string suffix, string url)
    {
        if (suffix is "_64" or "_thumb")
            photo.UrlThumbnail = url;
        else if (suffix is "_256" or "_medium")
            photo.UrlMedium = url;
        else
            photo.UrlFull = url;
    }
}
