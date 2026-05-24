using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyMarina.Application.Abstractions;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using MyMarina.Infrastructure.Storage;
using SkiaSharp;

namespace MyMarina.IntegrationTests;

[Collection("Integration")]
public class PhotosTests(ApiWebApplicationFactory factory)
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    async Task<(HttpClient client, Guid userId, Guid tenantId, Guid marinaId)> CreateMarinaWithOwnerAsync()
    {
        using var scope     = factory.Services.CreateScope();
        var db              = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager     = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var userId = Guid.CreateVersion7();
        var email  = $"photo-owner-{userId:N}@example.com";

        await userManager.CreateAsync(new ApplicationUser
        {
            Id = userId, UserName = email, Email = email,
            EmailConfirmed = true, FirstName = "Photo", LastName = "Owner",
        }, "TestPass!123");

        var tenant = new Domain.Entities.Tenant { Name = "Photo Tenant", Slug = $"photo-{userId:N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Photo Marina",
            Slug = $"photo-marina-{userId:N}", MarinaType = MarinaType.Commercial,
            IsSetupComplete = true,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);
        db.Memberships.Add(new Membership
        {
            UserId = userId, Scope = MembershipScope.Marina,
            TenantId = tenant.Id, MarinaId = marina.Id,
            Role = MembershipRole.Owner, AcceptedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var token  = TestJwtHelper.UserToken(userId, email,
            memberships: [new MembershipClaim(MembershipScope.Marina, tenant.Id, marina.Id, MembershipRole.Owner, null)]);
        var client = factory.CreateClientWithToken(token);

        return (client, userId, tenant.Id, marina.Id);
    }

    async Task<Guid> SeedGalleryPhotoAsync(Guid marinaId, Guid tenantId, int sortOrder = 0)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var photo = new MarinaPhoto
        {
            MarinaId = marinaId, TenantId = tenantId,
            Kind = MarinaPhotoKind.Gallery,
            StorageKey = $"test/{marinaId}/gallery/{Guid.CreateVersion7()}.jpg",
            UrlThumbnail = "https://example.com/thumb.jpg",
            UrlMedium    = "https://example.com/medium.jpg",
            UrlFull      = "https://example.com/full.jpg",
            SortOrder = sortOrder, Width = 1024, Height = 768, FileSizeBytes = 1000,
        };
        db.MarinaPhotos.Add(photo);
        await db.SaveChangesAsync();
        return photo.Id;
    }

    static byte[] CreateTestJpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SteelBlue);
        using var ms = new MemoryStream();
        bitmap.Encode(ms, SKEncodedImageFormat.Jpeg, 85);
        return ms.ToArray();
    }

    static MultipartFormDataContent CreatePhotoMultipart(
        byte[] imageBytes, string kind,
        string? caption = null, decimal? latitude = null, decimal? longitude = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(imageBytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "file", "photo.jpg");
        form.Add(new StringContent(kind), "kind");
        if (caption   is not null) form.Add(new StringContent(caption), "caption");
        if (latitude  is not null) form.Add(new StringContent(latitude.Value.ToString(CultureInfo.InvariantCulture)), "latitude");
        if (longitude is not null) form.Add(new StringContent(longitude.Value.ToString(CultureInfo.InvariantCulture)), "longitude");
        return form;
    }

    // ── POST /photos ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PostPhotos_ValidGalleryRequest_Returns201()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        var resp = await client.PostAsync($"/marinas/{marinaId}/photos",
            CreatePhotoMultipart(CreateTestJpeg(1024, 768), "Gallery"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PhotoBody>();
        Assert.NotNull(body?.Id);
        Assert.Equal("Gallery", body!.Kind);
    }

    [Fact]
    public async Task PostPhotos_NonMember_Returns403()
    {
        var (_, _, _, marinaId) = await CreateMarinaWithOwnerAsync();
        var stranger = factory.CreateClientWithToken(
            TestJwtHelper.UserToken(Guid.CreateVersion7(), "stranger@example.com"));

        var resp = await stranger.PostAsync($"/marinas/{marinaId}/photos",
            CreatePhotoMultipart(CreateTestJpeg(1024, 768), "Gallery"));

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task PostPhotos_OversizedFile_Returns413()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        // 200 KB exceeds MaxFileSizeBytes (100 KB in test config).
        // MultipartBodyLengthLimit = MaxFileSizeBytes * 5 = 500 KB, so the body reaches the
        // controller (which returns 413) rather than being rejected by middleware (which returns 400).
        var oversized = new byte[200_000];
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(oversized);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(fileContent, "file", "big.jpg");
        form.Add(new StringContent("Gallery"), "kind");

        var resp = await client.PostAsync($"/marinas/{marinaId}/photos", form);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, resp.StatusCode);
    }

    [Fact]
    public async Task PostPhotos_BadAspectRatioLogo_Returns422()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        // 1600×900 is 16:9 — ratio 1.78, outside Logo's ±10% of 1:1
        var resp = await client.PostAsync($"/marinas/{marinaId}/photos",
            CreatePhotoMultipart(CreateTestJpeg(1600, 900), "Logo"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task PostPhotos_DuplicateLogo_Returns409()
    {
        var (client, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.MarinaPhotos.Add(new MarinaPhoto
        {
            MarinaId = marinaId, TenantId = tenantId, Kind = MarinaPhotoKind.Logo,
            StorageKey = "test/logo.jpg", UrlThumbnail = "https://example.com/logo.jpg",
            SortOrder = 0, FileSizeBytes = 5000,
        });
        await db.SaveChangesAsync();

        // 512×512 is square — passes Logo aspect ratio; uniqueness check returns 409
        var resp = await client.PostAsync($"/marinas/{marinaId}/photos",
            CreatePhotoMultipart(CreateTestJpeg(512, 512), "Logo"));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── PATCH /{photoId}/reorder ──────────────────────────────────────────────

    [Fact]
    public async Task ReorderPhoto_Up_SwapsWithPreviousPhoto()
    {
        var (client, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();
        var id1 = await SeedGalleryPhotoAsync(marinaId, tenantId, sortOrder: 0);
        var id2 = await SeedGalleryPhotoAsync(marinaId, tenantId, sortOrder: 1);

        var resp = await client.PatchAsJsonAsync($"/marinas/{marinaId}/photos/{id2}/reorder",
            new { direction = "up" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var photos = await db.MarinaPhotos.Where(p => p.MarinaId == marinaId).ToListAsync();
        // Moving id2 "up" swaps it with id1: id2 takes sort order 0, id1 takes sort order 1
        Assert.Equal(0, photos.First(p => p.Id == id2).SortOrder);
        Assert.Equal(1, photos.First(p => p.Id == id1).SortOrder);
    }

    [Fact]
    public async Task ReorderPhoto_UpAtBoundary_Returns400()
    {
        var (client, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();
        var id1 = await SeedGalleryPhotoAsync(marinaId, tenantId, sortOrder: 0);

        var resp = await client.PatchAsJsonAsync($"/marinas/{marinaId}/photos/{id1}/reorder",
            new { direction = "up" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // ── DELETE /{photoId} ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePhoto_Owner_Returns204AndRecordRemoved()
    {
        var (client, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();
        var photoId = await SeedGalleryPhotoAsync(marinaId, tenantId);

        var resp = await client.DeleteAsync($"/marinas/{marinaId}/photos/{photoId}");

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = await db.MarinaPhotos.AnyAsync(p => p.Id == photoId);
        Assert.False(exists);
    }

    [Fact]
    public async Task DeletePhoto_NonMember_Returns403()
    {
        var (_, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();
        var photoId = await SeedGalleryPhotoAsync(marinaId, tenantId);

        var stranger = factory.CreateClientWithToken(
            TestJwtHelper.UserToken(Guid.CreateVersion7(), "stranger@example.com"));

        var resp = await stranger.DeleteAsync($"/marinas/{marinaId}/photos/{photoId}");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── GET /photos (anonymous) ───────────────────────────────────────────────

    [Fact]
    public async Task GetPhotos_Anonymous_Returns200()
    {
        var (_, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();
        await SeedGalleryPhotoAsync(marinaId, tenantId);

        var anonClient = factory.CreateClient();
        var resp = await anonClient.GetAsync($"/marinas/{marinaId}/photos");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PhotoBody[]>();
        Assert.NotNull(body);
        Assert.True(body!.Length >= 1);
    }

    // ── OrphanPhotoCleanupJob ─────────────────────────────────────────────────

    [Fact]
    public async Task OrphanCleanup_OldOrphan_IsRemovedRecentIsKept()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tenant = new Domain.Entities.Tenant { Name = "Orphan Tenant", Slug = $"orphan-{Guid.CreateVersion7():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Orphan Marina",
            Slug = $"orphan-marina-{Guid.CreateVersion7():N}", MarinaType = MarinaType.Commercial,
            IsSetupComplete = true,
        };
        db.Tenants.Add(tenant);
        db.Marinas.Add(marina);

        // Old orphan (no UrlFull, >1 hour old)
        var oldOrphan = new MarinaPhoto
        {
            MarinaId = marina.Id, TenantId = tenant.Id,
            Kind = MarinaPhotoKind.Gallery,
            StorageKey = $"test/orphan/old-{Guid.CreateVersion7()}.jpg",
            UploadedAt = DateTimeOffset.UtcNow.AddHours(-2),
            SortOrder = 0, FileSizeBytes = 100,
        };
        // Recent orphan (no UrlFull, <1 hour old) — should be kept
        var recentOrphan = new MarinaPhoto
        {
            MarinaId = marina.Id, TenantId = tenant.Id,
            Kind = MarinaPhotoKind.Gallery,
            StorageKey = $"test/orphan/new-{Guid.CreateVersion7()}.jpg",
            UploadedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            SortOrder = 1, FileSizeBytes = 100,
        };
        db.MarinaPhotos.AddRange(oldOrphan, recentOrphan);
        await db.SaveChangesAsync();

        var job = scope.ServiceProvider.GetRequiredService<OrphanPhotoCleanupJob>();
        await job.ExecuteAsync();

        var remaining = await db.MarinaPhotos
            .Where(p => p.MarinaId == marina.Id)
            .ToListAsync();

        Assert.DoesNotContain(remaining, p => p.Id == oldOrphan.Id);
        Assert.Contains(remaining, p => p.Id == recentOrphan.Id);
    }

    // ── Private DTO records ───────────────────────────────────────────────────

    private sealed record PhotoBody(string Id, string Kind);
}
