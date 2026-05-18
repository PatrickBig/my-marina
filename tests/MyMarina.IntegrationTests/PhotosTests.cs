using System.Net;
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

    // ── POST /ticket — task 17.3 ─────────────────────────────────────────────

    [Fact]
    public async Task PostTicket_ValidGalleryRequest_Returns200WithTicket()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        var resp = await client.PostAsJsonAsync($"/marinas/{marinaId}/photos/ticket", new
        {
            kind = "Gallery", contentType = "image/jpeg", fileSizeBytes = 1_000_000,
            imageWidth = 1024, imageHeight = 768,
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<TicketBody>();
        Assert.NotNull(body?.UploadUrl);
        Assert.NotNull(body?.Key);
    }

    [Fact]
    public async Task PostTicket_NonMember_Returns403()
    {
        var (_, _, _, marinaId) = await CreateMarinaWithOwnerAsync();
        var nonMemberClient = factory.CreateClientWithToken(
            TestJwtHelper.UserToken(Guid.CreateVersion7(), "stranger@example.com"));

        var resp = await nonMemberClient.PostAsJsonAsync($"/marinas/{marinaId}/photos/ticket", new
        {
            kind = "Gallery", contentType = "image/jpeg", fileSizeBytes = 500_000,
        });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task PostTicket_OversizedFile_Returns422()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        var resp = await client.PostAsJsonAsync($"/marinas/{marinaId}/photos/ticket", new
        {
            kind = "Gallery", contentType = "image/jpeg", fileSizeBytes = 25_000_000L, // > 20 MB
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task PostTicket_BadAspectRatioLogo_Returns422()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        var resp = await client.PostAsJsonAsync($"/marinas/{marinaId}/photos/ticket", new
        {
            kind = "Logo", contentType = "image/jpeg", fileSizeBytes = 500_000,
            imageWidth = 1600, imageHeight = 900, // 16:9 — not square
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resp.StatusCode);
    }

    [Fact]
    public async Task PostTicket_DuplicateLogo_Returns409()
    {
        var (client, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();

        // Seed an existing logo
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.MarinaPhotos.Add(new MarinaPhoto
        {
            MarinaId = marinaId, TenantId = tenantId, Kind = MarinaPhotoKind.Logo,
            StorageKey = "test/logo.jpg", UrlThumbnail = "https://example.com/logo.jpg",
            SortOrder = 0, FileSizeBytes = 5000,
        });
        await db.SaveChangesAsync();

        var resp = await client.PostAsJsonAsync($"/marinas/{marinaId}/photos/ticket", new
        {
            kind = "Logo", contentType = "image/jpeg", fileSizeBytes = 500_000,
            imageWidth = 512, imageHeight = 512,
        });

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    // ── POST /confirm — task 17.4 ─────────────────────────────────────────────

    [Fact]
    public async Task PostConfirm_MissingObject_Returns404()
    {
        var (client, _, _, marinaId) = await CreateMarinaWithOwnerAsync();

        var resp = await client.PostAsJsonAsync($"/marinas/{marinaId}/photos/confirm", new
        {
            key = "nonexistent/key/that/does/not/exist.jpg",
            kind = "Gallery",
        });

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task PostConfirm_WithRealFile_Returns201()
    {
        var (client, _, tenantId, marinaId) = await CreateMarinaWithOwnerAsync();

        // Pre-populate the in-memory store with a minimal 1×1 JPEG.
        // Use Logo kind (1:1 aspect ratio) so validation passes.
        var key = $"{tenantId}/{marinaId}/marina/logo/test-{Guid.CreateVersion7()}.jpg";
        var jpegBytes = Convert.FromBase64String(
            "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAFAABAAAAAAAAAAAAAAAAAAAACf/EABQQAQAAAAAAAAAAAAAAAAAAAAD/xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AJQAB/9k=");
        InMemoryStorageProvider.Objects[key] = (jpegBytes, "image/jpeg");

        var resp = await client.PostAsJsonAsync($"/marinas/{marinaId}/photos/confirm", new
        {
            key,
            kind = "Logo",
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<PhotoBody>();
        Assert.NotNull(body?.Id);
        Assert.Equal("Logo", body!.Kind);
    }

    // ── PATCH /{photoId}/reorder — task 17.5 ──────────────────────────────────

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

    // ── DELETE /{photoId} — task 17.6 ─────────────────────────────────────────

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

        var strangerClient = factory.CreateClientWithToken(
            TestJwtHelper.UserToken(Guid.CreateVersion7(), "stranger@example.com"));

        var resp = await strangerClient.DeleteAsync($"/marinas/{marinaId}/photos/{photoId}");

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

    // ── OrphanPhotoCleanupJob — task 17.7 ─────────────────────────────────────

    [Fact]
    public async Task OrphanCleanup_OldOrphan_IsRemovedRecentIsKept()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Find a demo marina or use a seeded marina for this test
        var tenant = new Domain.Entities.Tenant { Name = "Orphan Tenant", Slug = $"orphan-{Guid.CreateVersion7():N}" };
        var marina = new Domain.Entities.Marina
        {
            TenantId = tenant.Id, Name = "Orphan Marina",
            Slug = $"orphan-marina-{Guid.CreateVersion7():N}", MarinaType = MarinaType.Commercial,
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
        // Recent orphan (no UrlFull, < 1 hour old) — should be kept
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

        // Run the job directly
        var job = scope.ServiceProvider.GetRequiredService<OrphanPhotoCleanupJob>();
        await job.ExecuteAsync();

        var remaining = await db.MarinaPhotos
            .Where(p => p.MarinaId == marina.Id)
            .ToListAsync();

        Assert.DoesNotContain(remaining, p => p.Id == oldOrphan.Id);
        Assert.Contains(remaining, p => p.Id == recentOrphan.Id);
    }

    // ── Private DTO records for deserialization ───────────────────────────────
    private sealed record TicketBody(string UploadUrl, string Method, string Key);
    private sealed record PhotoBody(string Id, string Kind);
}
