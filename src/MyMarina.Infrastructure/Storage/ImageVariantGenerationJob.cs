using Hangfire;
using Microsoft.Extensions.Logging;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using SkiaSharp;

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
        var format = ext is "jpg" or "jpeg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
        var contentType = ext is "jpg" or "jpeg" ? "image/jpeg" : "image/png";

        var originalStream = await storage.GetObjectStreamAsync(originalKey);
        if (originalStream is null)
        {
            logger.LogWarning("ImageVariantGenerationJob: original not found for photo {PhotoId} key {Key}", photoId, originalKey);
            return;
        }

        await using (originalStream)
        {
            using var bitmap = SKBitmap.Decode(originalStream);
            if (bitmap is null)
            {
                logger.LogWarning("ImageVariantGenerationJob: failed to decode image for photo {PhotoId}", photoId);
                return;
            }

            photo.Width = bitmap.Width;
            photo.Height = bitmap.Height;

            if (photo.Kind == MarinaPhotoKind.Logo)
            {
                foreach (var (suffix, size) in SquareSizes)
                {
                    var variantKey = $"{keyWithoutExt}{suffix}.{ext}";
                    var bytes = CropSquare(bitmap, size, format);
                    await SaveVariantAsync(variantKey, bytes, contentType);
                    SetVariantUrl(photo, suffix, storage.GetPublicUrl(variantKey));
                }
            }
            else
            {
                foreach (var (suffix, width) in LandscapeWidths)
                {
                    var variantKey = $"{keyWithoutExt}{suffix}.{ext}";
                    var bytes = ResizeMaxWidth(bitmap, width, format);
                    await SaveVariantAsync(variantKey, bytes, contentType);
                    SetVariantUrl(photo, suffix, storage.GetPublicUrl(variantKey));
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("ImageVariantGenerationJob: variants generated for photo {PhotoId}", photoId);
    }

    private static byte[] CropSquare(SKBitmap source, int size, SKEncodedImageFormat format)
    {
        int dim = Math.Min(source.Width, source.Height);
        int x = (source.Width - dim) / 2;
        int y = (source.Height - dim) / 2;

        // ExtractSubset creates a view sharing source memory — Resize immediately creates an independent copy
        using var subset = new SKBitmap();
        source.ExtractSubset(subset, new SKRectI(x, y, x + dim, y + dim));

        using var resized = subset.Resize(new SKImageInfo(size, size), new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new InvalidOperationException($"Failed to resize image to {size}x{size}.");
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(format, 85);
        return data.ToArray();
    }

    private static byte[] ResizeMaxWidth(SKBitmap source, int maxWidth, SKEncodedImageFormat format)
    {
        SKBitmap bitmap;
        bool owned;
        if (source.Width <= maxWidth)
        {
            bitmap = source;
            owned = false;
        }
        else
        {
            int height = (int)Math.Round((double)source.Height * maxWidth / source.Width);
            bitmap = source.Resize(new SKImageInfo(maxWidth, height), new SKSamplingOptions(SKCubicResampler.Mitchell))
                ?? throw new InvalidOperationException($"Failed to resize image to {maxWidth}px wide.");
            owned = true;
        }

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(format, 85);
            return data.ToArray();
        }
        finally
        {
            if (owned) bitmap.Dispose();
        }
    }

    private async Task SaveVariantAsync(string key, byte[] bytes, string contentType)
    {
        using var ms = new MemoryStream(bytes);
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
