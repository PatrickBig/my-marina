## Context

Marina listings currently have no photo fields anywhere in the domain — `Marina`, `Dock`, and `Slip` entities carry only text and structured data. Boaters browsing the marketplace see name, location, and dimensions but no visual context, which undermines trust and conversion. This change introduces the foundational photo infrastructure: a pluggable storage abstraction, a `MarinaPhoto` entity covering marina-level photo kinds, and the operator UI to manage them. Slip and dock photos are deferred to a follow-on change (`slip-dock-photos-marketplace`).

The system must work identically against Cloudflare R2 (production) and RustFS (local docker-compose), without requiring the frontend or application layer to know which provider is active.

## Goals / Non-Goals

**Goals:**
- Pluggable `IStorageProvider` abstraction that hides storage vendor from application code
- Presigned URL upload flow so large files never transit the API server
- Server-side image processing (SkiaSharp) via Hangfire to generate size variants
- `MarinaPhoto` entity supporting kinds: Logo, Banner, Gallery, Aerial, Approach
- Approach photos support optional caption + lat/lng for navigation context
- Marina operator UI: crop-on-upload, gallery management, up/down reordering
- Logo and Banner embedded in marina search API response
- New "Photos" step in the onboarding wizard (optional, logo/banner focused)
- Demo seed populated with Picsum placeholder photos for all kinds

**Non-Goals:**
- Dock or slip photos (follow-on change)
- Photo moderation / flag-and-review system (follow-on change)
- Tier gating on photo counts (pricing model not yet determined)
- CDN configuration / cache-control headers (operational concern, post-MVP)
- 360° / virtual tour photos

## Decisions

### Decision 1 — Storage abstraction via `IStorageProvider`

```
IStorageProvider
├── CreateUploadTicketAsync(key, contentType, maxBytes, expiresIn) → UploadTicket
├── ConfirmUploadAsync(key) → StoredFileInfo (width, height, sizeBytes)
├── GetPublicUrl(key) → string
├── DeleteAsync(key)
└── DeleteByPrefixAsync(prefix)   ← for cascading marina/entity cleanup

UploadTicket
├── UploadUrl       ← presigned PUT URL
├── Method          ← "PUT"
├── RequiredHeaders ← Content-Type required by S3
├── Key
└── ExpiresAt
```

**S3Provider**: Uses `AWSSDK.S3` with a configurable endpoint — same code for R2, RustFS, and AWS by setting `Endpoint` + credentials in config. `ForcePathStyle = true` for non-AWS endpoints. Presigned PUT URLs are generated via `GetPreSignedUrlRequest`. The `PublicEndpoint` config value rewrites the presigned URL origin so the browser sees a host-accessible address (e.g., `localhost:9000`) while the API uses the internal Docker service name. On confirm, the provider calls `GetObjectMetadataRequest` to verify the object exists and retrieve content length.

**Registration**: `S3StorageProvider` registered as `IStorageProvider` via Scrutor when `appsettings.json → Storage:Provider` is `"S3"` (the only supported value).

### Decision 2 — Upload flow (presigned, not proxied)

```
1. Client → POST /marinas/{id}/photos/ticket  { kind, contentType, fileSizeBytes }
   ← { uploadUrl, method, requiredHeaders, key, expiresAt }
2. Client → PUT/POST {uploadUrl}  (direct to storage, bypasses API)
3. Client → POST /marinas/{id}/photos/confirm  { key }
   ← MarinaPhotoDto (placeholder URLs until variants ready)
4. Hangfire: ImageVariantGenerationJob runs → updates variant URL columns
5. Client polls or uses SignalR notification (v1: poll on page load is fine)
```

The API never buffers the upload payload — the browser PUTs directly to the storage endpoint via the presigned URL.

### Decision 3 — Image processing: Hangfire + SkiaSharp

SkiaSharp runs in-process in the Hangfire worker. The `ImageVariantGenerationJob` is enqueued immediately on confirm and is idempotent — safe to retry.

**Two variant sets keyed by `MarinaPhotoKind`:**

| Set | Applies to | Variants |
|---|---|---|
| Square | Logo | `_64` (64×64), `_256` (256×256), `_512` (512×512) |
| Landscape | Banner, Gallery, Aerial, Approach | `_thumb` (400w), `_medium` (800w), `_full` (2000w) |

Landscape variants preserve aspect ratio (width constrained, height proportional). Square variants center-crop then resize. Originals are retained under `{key}` as uploaded.

**Variant storage keys**: `{original_key}_thumb`, `{original_key}_medium`, `{original_key}_full` (appended before extension stripped). E.g., `abc/def/marina/gallery/photo-id_medium.jpg`.

### Decision 4 — Storage key convention

```
{tenantId}/{marinaId}/marina/logo/{photoId}.jpg
{tenantId}/{marinaId}/marina/banner/{photoId}.jpg
{tenantId}/{marinaId}/marina/gallery/{photoId}.jpg
{tenantId}/{marinaId}/marina/aerial/{photoId}.jpg
{tenantId}/{marinaId}/marina/approach/{photoId}.jpg
```

`tenantId` prefix enables per-tenant storage permission scoping and makes prefix-based cleanup safe (delete a marina → `DeleteByPrefixAsync("{tenantId}/{marinaId}/")`).

### Decision 5 — `MarinaPhoto` entity (single table, kind discriminator)

```csharp
public class MarinaPhoto
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }          // for global query filter
    public Guid MarinaId { get; set; }
    public MarinaPhotoKind Kind { get; set; }
    public string StorageKey { get; set; }       // canonical, not CDN URL
    public string? UrlFull { get; set; }
    public string? UrlMedium { get; set; }
    public string? UrlThumbnail { get; set; }
    public int SortOrder { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Caption { get; set; }         // Approach photos
    public decimal? Latitude { get; set; }       // Approach photos (optional)
    public decimal? Longitude { get; set; }      // Approach photos (optional)
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; init; } = DateTimeOffset.UtcNow;
    public Marina Marina { get; set; } = null!;
}

public enum MarinaPhotoKind { Logo = 0, Banner = 1, Gallery = 2, Aerial = 3, Approach = 4 }
```

EF config enforces: unique index on `(MarinaId, Kind)` filtered to `Kind IN (Logo, Banner)` — prevents duplicate logo/banner at the DB level. `TenantId` participates in the global query filter.

A single table avoids the complexity of per-kind tables while retaining FK integrity for the `MarinaId` relationship. `Caption`, `Latitude`, `Longitude` are nullable and only meaningful for `Kind = Approach` — no polymorphic table needed.

### Decision 6 — Aspect ratio validation (server-side)

Validation runs in the upload-ticket handler (before issuing the presigned URL, if dimensions are known) **and** again in the confirm handler (after `ConfirmUploadAsync` returns actual dimensions). Client-side crop enforces the same rules but is not trusted.

| Kind | Rule | Server check |
|---|---|---|
| Logo | 1:1 ±10% | `abs(width/height - 1.0) < 0.10` |
| Banner | 16:9 ±15% | `abs(width/height - 1.778) < 0.267` |
| Gallery, Aerial, Approach | min 800px wide | `width >= 800` |

Ticket request includes `imageWidth` and `imageHeight` (from the crop UI, before upload). If dimensions violate the rule, the API returns 422 with a descriptive error before a presigned URL is issued — saves the round-trip.

### Decision 7 — Authorization

Upload/manage photos: `IUserContext.HasMarinaAccess(marinaId)` — any `MembershipRole` (Staff/Manager/Owner) at the marina qualifies. Renters/boaters hold `BillingAccountMember` records only, not `Membership` records, so they naturally cannot pass this check. Platform operators (`IUserContext.IsPlatformOperator`) can manage photos for any marina.

### Decision 8 — Reorder mechanism

The `PATCH /marinas/{id}/photos/{photoId}/reorder` endpoint accepts `{ direction: "up" | "down" }`. The handler loads the adjacent photo (by `SortOrder`) and swaps the two `SortOrder` values in a single transaction. The client renders up/down buttons; the first photo's up button and last photo's down button are disabled. This avoids drag-and-drop complexity while keeping `SortOrder` as a generic integer that can support DnD later without a schema change.

### Decision 9 — Onboarding wizard integration

The wizard currently has 5 steps (`SetupStep` 1–5). The new Photos step is inserted as Step 5; the existing Publish step becomes Step 6. `Marina.SetupStep` is set to 5 when the operator lands on the Photos step. Skipping (clicking "Skip for now") advances `SetupStep` to 6 directly.

For marinas already at `SetupStep = 5` (Publish) from the prior wizard: since the product is not live, any existing demo/test data will be re-seeded with `IsSetupComplete = true`, so the step integer shift has no effect on real users.

The Photos step UI:
- Logo upload slot (prominent, with crop modal)
- Banner upload slot (with crop modal)
- "Skip for now" link (muted, bottom of form)
- Progress indicator shows step 5 of 6
- On completion or skip → advance to Step 6 (Publish)

## Risks / Trade-offs

**[Risk] Variants not ready when confirm response is returned** → Mitigation: `MarinaPhotoDto` returns `null` for variant URL fields until the Hangfire job completes. The frontend renders a loading skeleton for the photo card. Polling on page load (re-fetch photos after 2s if any `urlThumbnail` is null) is acceptable for v1. SignalR push is a future improvement.

**[Risk] Presigned URL expiry if user takes too long to upload** → Mitigation: Ticket expiry set to 15 minutes (configurable). If the upload fails with 403, the client re-requests a ticket and retries automatically.

**[Risk] Large files blocking the Hangfire worker thread** → Mitigation: SkiaSharp processes synchronously on the Hangfire thread but the job is queued in a dedicated `"photos"` queue with its own worker count (configurable). Upstream file size is capped at 20 MB at the ticket-request layer.

**[Risk] Orphaned storage objects if the confirm step is never called** → Mitigation: A nightly Hangfire recurring job scans for `MarinaPhoto` records with `UrlFull = null` older than 1 hour and deletes their storage key. Additionally, storage objects with no matching DB record older than 24 hours can be pruned by prefix scan.

## Migration Plan

1. Add `MarinaPhoto` EF migration (no breaking changes to existing tables).
2. Add `"Storage"` section to `appsettings.Development.json` pointing at RustFS.
3. Add RustFS to `docker-compose.yml`; run `docker-compose up` to verify.
4. Update `DemoSeedScript` with placeholder photos; run CI seed test.
5. Deploy: no existing data migration needed (new table only).
6. Rollback: drop `MarinaPhoto` table; remove RustFS from compose; no other state affected.

## Open Questions

- *(Resolved)* Storage backend → Cloudflare R2 for production, RustFS for local dev. FilesystemProvider dropped — S3-compatible object stores cover all environments.
- *(Resolved)* Variant sets → two sets (Square / Landscape) keyed by kind.
- *(Resolved)* Wizard step → optional with encouragement, logo/banner focused.
- **Photo deletion cascade**: When a `Marina` is deleted (currently only allowed for draft marinas), should all `MarinaPhoto` storage objects be deleted synchronously or via a queued job? → Recommend queued job for consistency with the confirm/delete pattern.
