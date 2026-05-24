## 1. Dependencies & Configuration

- [x] 1.1 Add `SixLabors.ImageSharp` NuGet package to `MyMarina.Infrastructure`
- [x] 1.2 Add `AWSSDK.S3` NuGet package to `MyMarina.Infrastructure` (if not already present)
- [x] 1.3 Add `react-image-crop` npm package to `src/MyMarina.Web`
- [x] 1.4 Add `"Storage"` config section to `appsettings.json` with `Provider`, `S3`, and `Filesystem` sub-sections (keys only, no secrets)
- [x] 1.5 Add `"Storage"` section to `appsettings.Development.json` pointing at MinIO (`http://localhost:9000`, bucket `mymarina-local`)
- [x] 1.6 Add MinIO service to `docker-compose.yml` (image `minio/minio`, S3 API on port 9000, console on port 9001, data volume, `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` env vars)
- [x] 1.7 Add `appsettings.Test.json` or test fixture config pointing at a MinIO test bucket for integration tests

## 2. Storage Abstraction — Core Interfaces & Records

- [x] 2.1 Create `MyMarina.Infrastructure/Storage/IStorageProvider.cs` with methods: `CreateUploadTicketAsync`, `ConfirmUploadAsync`, `GetPublicUrl`, `DeleteAsync`, `DeleteByPrefixAsync`
- [x] 2.2 Create `UploadTicket` record (`UploadUrl`, `Method`, `RequiredHeaders`, `Key`, `ExpiresAt`) in `MyMarina.Infrastructure/Storage/`
- [x] 2.3 Create `StoredFileInfo` record (`FileSizeBytes`) in `MyMarina.Infrastructure/Storage/`
- [x] 2.4 Create `StorageOptions` POCO with `Provider` (string), `S3` sub-options (`Endpoint`, `Bucket`, `AccessKey`, `SecretKey`, `BucketPublicBaseUrl`), `Filesystem` sub-options (`BasePath`, `BaseUrl`)
- [x] 2.5 Register `StorageOptions` with `services.Configure<StorageOptions>(config.GetSection("Storage"))` in the Infrastructure service registration

## 3. S3 Storage Provider

- [x] 3.1 Create `S3StorageProvider : IStorageProvider` in `MyMarina.Infrastructure/Storage/`
- [x] 3.2 Implement `CreateUploadTicketAsync` — build `GetPreSignedUrlRequest` with `PUT` verb, configured TTL, and `ContentType`; return `UploadTicket` with `Method = "PUT"` and `RequiredHeaders = { "Content-Type": contentType }`
- [x] 3.3 Implement `ConfirmUploadAsync` — call `GetObjectMetadataRequest`; return `StoredFileInfo` with `ContentLength`; throw `StorageObjectNotFoundException` if object missing
- [x] 3.4 Implement `GetPublicUrl(key)` — return `{BucketPublicBaseUrl}/{key}`
- [x] 3.5 Implement `DeleteAsync` — call `DeleteObjectRequest`; swallow `NoSuchKey` error
- [x] 3.6 Implement `DeleteByPrefixAsync` — list objects by prefix using `ListObjectsV2Request`, batch-delete via `DeleteObjectsRequest` in groups of 1000

## 4. Filesystem Storage Provider

- [x] 4.1 Create `FilesystemStorageProvider : IStorageProvider` in `MyMarina.Infrastructure/Storage/`
- [x] 4.2 Implement `CreateUploadTicketAsync` — generate a signed single-use JWT upload token (claims: `key`, `exp`); return `UploadTicket` with `UploadUrl = /api/photos/local-upload/{token}` and `Method = "POST"`
- [x] 4.3 Implement `ConfirmUploadAsync` — verify file exists at `{BasePath}/{key}`; return `StoredFileInfo` with file length; throw `StorageObjectNotFoundException` if absent
- [x] 4.4 Implement `GetPublicUrl(key)` — return `{BaseUrl}/{key}`
- [x] 4.5 Implement `DeleteAsync` — delete file at `{BasePath}/{key}`; no-op if not found
- [x] 4.6 Implement `DeleteByPrefixAsync` — enumerate files under `{BasePath}/{prefix}` and delete each
- [x] 4.7 Create `LocalUploadController` in `MyMarina.Api/Controllers/` with `POST /api/photos/local-upload/{token}` — validate JWT token (not replayed, not expired), stream request body to `{BasePath}/{key}` without buffering, mark token consumed in `IMemoryCache`

## 5. Provider Registration

- [x] 5.1 In the Infrastructure DI registration, read `Storage:Provider` from configuration and conditionally register either `S3StorageProvider` or `FilesystemStorageProvider` as `IStorageProvider` (singleton)
- [x] 5.2 Throw `InvalidOperationException` at startup if `Storage:Provider` is missing or unrecognised

## 6. Domain — MarinaPhoto Entity & Enum

- [x] 6.1 Create `MarinaPhotoKind` enum (`Logo=0`, `Banner=1`, `Gallery=2`, `Aerial=3`, `Approach=4`) in `MyMarina.Domain/Enums/`
- [x] 6.2 Create `MarinaPhoto` entity in `MyMarina.Domain/Entities/` with all fields from the design (Id, TenantId, MarinaId, Kind, StorageKey, UrlFull, UrlMedium, UrlThumbnail, SortOrder, Width, Height, FileSizeBytes, Caption, Latitude, Longitude, UploadedByUserId, UploadedAt)
- [x] 6.3 Add `MarinaPhoto` navigation collection to `Marina` entity

## 7. Infrastructure — EF Core Configuration & Migration

- [x] 7.1 Create `MarinaPhotoConfiguration : IEntityTypeConfiguration<MarinaPhoto>` — configure table name, primary key, FK to Marina, global query filter on `TenantId`, unique filtered index on `(MarinaId, Kind)` where `Kind IN (0, 1)` (Logo and Banner)
- [x] 7.2 Register `MarinaPhotoConfiguration` in `AppDbContext`
- [x] 7.3 Run `dotnet ef migrations add Phase18_MarinaPhotos --project src/MyMarina.Infrastructure --startup-project src/MyMarina.Api` to generate the migration
- [x] 7.4 Verify the generated migration contains `CreateTable("MarinaPhotos", ...)` and the filtered unique index — do not manually edit the migration file

## 8. Infrastructure — Hangfire Jobs

- [x] 8.1 Create `ImageVariantGenerationJob` in `MyMarina.Infrastructure/Storage/` — accept `photoId` (Guid); load `MarinaPhoto`; download original from `IStorageProvider`; use ImageSharp to generate variant set based on `Kind` (Square for Logo, Landscape for all others); upload each variant; update `MarinaPhoto.UrlFull/Medium/Thumbnail` and `Width/Height`; save changes
- [x] 8.2 Create `StorageCleanupJob` in `MyMarina.Infrastructure/Storage/` — accept `storageKey` (string); call `IStorageProvider.DeleteAsync` for original key and `{key}_thumb`, `{key}_medium`, `{key}_full` variants; idempotent (swallow not-found)
- [x] 8.3 Create `OrphanPhotoCleanupJob` (recurring) — query `MarinaPhoto` records with `UrlFull = null` and `UploadedAt < UtcNow - 1 hour`; for each: enqueue `StorageCleanupJob` and delete the DB record; register as nightly Hangfire recurring job
- [x] 8.4 Register dedicated Hangfire queue `"photos"` in Hangfire configuration

## 9. Application — Photo Upload & Management Handlers

- [x] 9.1 Create `CreateUploadTicketCommand` with `MarinaId`, `Kind`, `ContentType`, `FileSizeBytes`, `ImageWidth?`, `ImageHeight?` and `CreateUploadTicketCommandHandler` — validate membership, file size, aspect ratio; call `IStorageProvider.CreateUploadTicketAsync`; return `UploadTicket`
- [x] 9.2 Add `AspectRatioValidator` static helper — given `Kind`, `width`, `height`, return `(bool valid, string? errorMessage)` per rules in design
- [x] 9.3 Create `ConfirmPhotoUploadCommand` with `MarinaId`, `Key`, `Kind`, `Caption?`, `Latitude?`, `Longitude?` and `ConfirmPhotoUploadCommandHandler` — call `ConfirmUploadAsync`; read image dimensions via ImageSharp metadata; re-validate aspect ratio; create `MarinaPhoto` record; enqueue `ImageVariantGenerationJob`; return `MarinaPhotoDto`
- [x] 9.4 Create `ReorderPhotoCommand` with `MarinaId`, `PhotoId`, `Direction` (enum: Up/Down) and handler — load adjacent photo within same Kind; swap SortOrder; return 400 if already at boundary
- [x] 9.5 Create `DeletePhotoCommand` with `MarinaId`, `PhotoId` and handler — delete `MarinaPhoto` record; enqueue `StorageCleanupJob` with the photo's `StorageKey`
- [x] 9.6 Create `GetMarinaPhotosQuery` with `MarinaId` and handler — return all `MarinaPhoto` records ordered by `Kind` then `SortOrder`

## 10. API — PhotosController

- [x] 10.1 Create `PhotosController` in `MyMarina.Api/Controllers/` with base route `/api/marinas/{marinaId}/photos`
- [x] 10.2 Add `POST /ticket` action — require marina membership; delegate to `CreateUploadTicketCommandHandler`; return 200 with `UploadTicket` or 422 on validation failure
- [x] 10.3 Add `POST /confirm` action — require marina membership; delegate to `ConfirmPhotoUploadCommandHandler`; return 201 with `MarinaPhotoDto`
- [x] 10.4 Add `GET /` action — allow any authenticated user (or public for listed marinas); delegate to `GetMarinaPhotosQuery`; return 200 with `MarinaPhotoDto[]`
- [x] 10.5 Add `PATCH /{photoId}/reorder` action — require marina membership; delegate to `ReorderPhotoCommand`; return 200 or 400
- [x] 10.6 Add `DELETE /{photoId}` action — require marina membership OR platform operator; delegate to `DeletePhotoCommand`; return 204
- [x] 10.7 Create `MarinaPhotoDto` with all photo fields (id, kind, urlFull, urlMedium, urlThumbnail, sortOrder, caption, latitude, longitude, uploadedAt)

## 11. API — Marina Search & Detail DTOs

- [x] 11.1 Update the marina search result DTO to include `logoUrl` (string?) and `bannerThumbnailUrl` (string?)
- [x] 11.2 Update the marina detail DTO to include `logoUrl` and `bannerThumbnailUrl`
- [x] 11.3 Update the marina search/detail query handlers to LEFT JOIN `MarinaPhotos` for Logo and Banner kinds and populate the new URL fields from `UrlThumbnail` (Logo) and `UrlMedium` (Banner)
- [x] 11.4 Run `npm run generate-api` against the updated running API to regenerate `src/MyMarina.Web/src/api/schema.d.ts`

## 12. Demo Seed

- [x] 12.1 Update `DemoSeedScript.SeedAsync` to insert `MarinaPhoto` records for the demo marina: 1 Logo, 1 Banner, 3 Gallery, 1 Aerial, 2 Approach (with captions and one with lat/lng)
- [x] 12.2 Use Picsum deterministic URLs for all variant fields (e.g., `https://picsum.photos/seed/demo-marina-logo/256/256` for Logo thumbnail). Set `StorageKey` to a placeholder string (e.g., `demo/marina/logo/seed-photo.jpg`)
- [x] 12.3 Verify the CI integration test asserting entity presence now includes `MarinaPhoto`

## 13. Frontend — Shared Upload Infrastructure

- [x] 13.1 Create `usePhotoUpload(marinaId)` hook in `src/MyMarina.Web/src/hooks/` — encapsulates: request ticket, upload to `UploadTicket.UploadUrl` using the correct HTTP method and `RequiredHeaders`, call confirm endpoint, return the created `MarinaPhotoDto`
- [x] 13.2 Create `CropUploadModal` component (`src/MyMarina.Web/src/components/CropUploadModal.tsx`) — accepts `aspectRatio?`, `onComplete(file: Blob)` prop; renders `react-image-crop` in a modal; on confirm, invokes `onComplete` with the cropped canvas blob
- [x] 13.3 Create `PhotoCard` component — renders a photo tile with a loading skeleton when `urlThumbnail` is null; up/down reorder buttons; delete button with confirmation

## 14. Frontend — Marina Photo Management Page/Tab

- [x] 14.1 Add a "Photos" section or tab to the marina settings page (or create `/marina/{id}/photos` route)
- [x] 14.2 Implement Logo slot — shows current logo (256px) or empty placeholder; clicking opens `CropUploadModal` with `aspectRatio = 1`; on crop complete, calls `usePhotoUpload` for `kind = Logo`; replaces existing logo (delete-then-upload if one exists)
- [x] 14.3 Implement Banner slot — shows current banner (800w) or empty placeholder; clicking opens `CropUploadModal` with `aspectRatio = 16/9`; on crop complete, calls `usePhotoUpload` for `kind = Banner`
- [x] 14.4 Implement Gallery tab — photo grid with "Add photo" button (no crop enforcement, min 800px wide); each `PhotoCard` shows up/down buttons and delete; calls reorder/delete API endpoints
- [x] 14.5 Implement Aerial tab — same as Gallery tab but for `kind = Aerial`
- [x] 14.6 Implement Approach tab — same as Gallery tab for `kind = Approach`; each photo card also shows a Caption input (persisted on blur via PATCH) and optional Latitude/Longitude fields
- [x] 14.7 On page load, poll once after 2 seconds for any photos with `urlThumbnail = null` to detect completed variant jobs; update the photo list if any have been processed

## 15. Frontend — Onboarding Wizard Step 5

- [x] 15.1 Update `MarinaSetupWizardPage.tsx` — add Step 5 "Photos" between Step 4 (Preview & Adjust) and the existing Publish step (now Step 6); update progress indicator to show "Step X of 6"
- [x] 15.2 Implement Step 5 UI — Logo upload slot; Banner upload slot; encouragement copy ("Marinas with photos receive significantly more inquiries"); muted "Skip for now — add photos later from your marina settings" link at bottom
- [x] 15.3 Wire "Skip for now" to PATCH marina with `setupStep: 6` and advance wizard to Step 6
- [x] 15.4 Wire photo uploads on this step to `usePhotoUpload` (same hook as the management page)
- [x] 15.5 On "Next" (with or without uploads), PATCH marina with `setupStep: 5` (or 6 if skipping) and navigate to Publish step

## 16. Frontend — Marina Search Cards & Detail Page

- [x] 16.1 Update marina search result card to render `logoUrl` as a circular avatar (or placeholder icon) and `bannerThumbnailUrl` as the card hero image when present
- [x] 16.2 Update marina detail page to render the full banner image at the top and the logo avatar in the marina header section
- [x] 16.3 Add "Getting Here" section to marina detail page — renders Approach photos in SortOrder with captions; shows Leaflet map with pins if any Approach photo has coordinates; hides section when no Approach photos exist

## 17. Testing

- [x] 17.1 Add unit tests for `AspectRatioValidator` — all five kinds, boundary values, valid and invalid inputs
- [x] 17.2 Add unit test for `ImageVariantGenerationJob` — mock `IStorageProvider`; assert correct variant keys written for Square vs Landscape sets
- [x] 17.3 Add integration test: `POST /ticket` — valid request returns ticket; non-member returns 403; oversized file returns 422; bad aspect ratio returns 422; duplicate logo returns 409
- [x] 17.4 Add integration test: `POST /confirm` — object exists → photo created and job enqueued; object missing → 404
- [x] 17.5 Add integration test: `PATCH /{photoId}/reorder` — up/down swaps correctly; boundary returns 400
- [x] 17.6 Add integration test: `DELETE /{photoId}` — record deleted; storage cleanup job enqueued
- [x] 17.7 Add integration test: `OrphanPhotoCleanupJob` — orphan photo older than 1 hour is removed; recent orphan is left alone
- [x] 17.8 Add integration test: demo seed CI assertion now includes `MarinaPhoto`
- [x] 17.9 Capture Playwright screenshots of the Photos step in the onboarding wizard and the marina photo management UI; commit to `src/MyMarina.Marketing/public/screenshots/` using the `playwright-cli` skill
