using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;
using SkiaSharp;

namespace MyMarina.UnitTests;

public class ImageVariantGenerationJobTests
{
    // ── Logo (1:1 Square variants) ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_LogoPhoto_WritesThreeSquareVariants()
    {
        var (db, storage, photo) = await SetupAsync(MarinaPhotoKind.Logo, "logo/photo.jpg");

        var job = new ImageVariantGenerationJob(db, storage, NullLogger<ImageVariantGenerationJob>.Instance);
        await job.ExecuteAsync(photo.Id);

        var updated = await db.MarinaPhotos.FindAsync(photo.Id);
        Assert.NotNull(updated!.UrlThumbnail); // _64
        Assert.NotNull(updated.UrlMedium);     // _256
        Assert.NotNull(updated.UrlFull);       // _512

        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_64.jpg"));
        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_256.jpg"));
        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_512.jpg"));
        Assert.Equal(3, storage.WrittenKeys.Count);
    }

    [Fact]
    public async Task ExecuteAsync_LogoPhoto_SetsWidthAndHeight()
    {
        var (db, storage, photo) = await SetupAsync(MarinaPhotoKind.Logo, "logo/photo.jpg", width: 300, height: 200);

        var job = new ImageVariantGenerationJob(db, storage, NullLogger<ImageVariantGenerationJob>.Instance);
        await job.ExecuteAsync(photo.Id);

        var updated = await db.MarinaPhotos.FindAsync(photo.Id);
        Assert.Equal(300, updated!.Width);
        Assert.Equal(200, updated.Height);
    }

    // ── Banner (Landscape variants) ─────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_BannerPhoto_WritesThreeLandscapeVariants()
    {
        var (db, storage, photo) = await SetupAsync(MarinaPhotoKind.Banner, "banner/photo.jpg");

        var job = new ImageVariantGenerationJob(db, storage, NullLogger<ImageVariantGenerationJob>.Instance);
        await job.ExecuteAsync(photo.Id);

        var updated = await db.MarinaPhotos.FindAsync(photo.Id);
        Assert.NotNull(updated!.UrlThumbnail); // _thumb
        Assert.NotNull(updated.UrlMedium);     // _medium
        Assert.NotNull(updated.UrlFull);       // _full

        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_thumb.jpg"));
        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_medium.jpg"));
        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_full.jpg"));
        Assert.Equal(3, storage.WrittenKeys.Count);
    }

    [Fact]
    public async Task ExecuteAsync_GalleryPhoto_WritesThreeLandscapeVariants()
    {
        var (db, storage, photo) = await SetupAsync(MarinaPhotoKind.Gallery, "gallery/photo.png", "image/png");

        var job = new ImageVariantGenerationJob(db, storage, NullLogger<ImageVariantGenerationJob>.Instance);
        await job.ExecuteAsync(photo.Id);

        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_thumb.png"));
        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_medium.png"));
        Assert.Contains(storage.WrittenKeys, k => k.EndsWith("_full.png"));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownPhotoId_DoesNotThrow()
    {
        var db = CreateDbContext();
        var storage = new CapturingStorageProvider(CreateJpeg(100, 100));
        var job = new ImageVariantGenerationJob(db, storage, NullLogger<ImageVariantGenerationJob>.Instance);

        await job.ExecuteAsync(Guid.NewGuid()); // no-op, logs warning

        Assert.Empty(storage.WrittenKeys);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static async Task<(AppDbContext Db, CapturingStorageProvider Storage, MarinaPhoto Photo)> SetupAsync(
        MarinaPhotoKind kind,
        string relativeKey,
        string contentType = "image/jpeg",
        int width = 500,
        int height = 300)
    {
        var db = CreateDbContext();
        var storageKey = $"tenant/marina/marina/{relativeKey}";
        var photo = new MarinaPhoto
        {
            MarinaId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Kind = kind,
            StorageKey = storageKey,
        };
        db.MarinaPhotos.Add(photo);
        await db.SaveChangesAsync();

        var format = contentType == "image/png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        var storage = new CapturingStorageProvider(CreateImage(width, height, format));
        return (db, storage, photo);
    }

    private static AppDbContext CreateDbContext()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static byte[] CreateJpeg(int width, int height) =>
        CreateImage(width, height, SKEncodedImageFormat.Jpeg);

    private static byte[] CreateImage(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 80);
        return data.ToArray();
    }
}

/// <summary>
/// IStorageProvider test double that captures written keys and serves a fixed image stream.
/// </summary>
internal sealed class CapturingStorageProvider(byte[] imageBytes) : IStorageProvider
{
    public List<string> WrittenKeys { get; } = [];

    public Task<Stream?> GetObjectStreamAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream?>(new MemoryStream(imageBytes));

    public Task PutObjectStreamAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        WrittenKeys.Add(key);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string key) => $"https://storage.example.com/{key}";

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteByPrefixAsync(string prefix, CancellationToken ct = default)
        => Task.CompletedTask;
}
