## Why

Marina listings on the marketplace have no visual identity — no logo, no banner, no photos — making them indistinguishable from one another and reducing boater confidence. Providing a first-class photo infrastructure (with direct-to-storage upload, server-side image processing, and a purpose-built gallery management UI) lets marina operators brand their listings and give boaters the visual context they need before booking.

## What Changes

- **New: Storage abstraction** — `IStorageProvider` interface with two implementations: `S3StorageProvider` (works with Cloudflare R2, MinIO, and AWS S3) and `FilesystemStorageProvider` (k8s NFS / local disk). Provider is selected via `appsettings.json` `"Storage"` config section.
- **New: MinIO in docker-compose** — local dev uses MinIO (S3-compatible) so the presigned-URL upload path is exercised identically to production.
- **New: MarinaPhoto entity** — stores photo metadata (kind, storage key, CDN variant URLs, sort order, dimensions, uploader). Kinds in this change: `Logo`, `Banner`, `Gallery`, `Aerial`, `Approach`.
- **New: Approach photos** — a distinct photo kind with optional caption and optional lat/lng; shown to boaters in a "Getting Here" section to aid navigation.
- **New: Presigned upload flow** — API issues a short-lived signed URL; browser uploads directly to storage; API confirms and creates the `MarinaPhoto` record.
- **New: ImageSharp variant generation** — Hangfire background job generates two variant sets after upload: Square (64/256/512px) for Logo; Landscape (thumb 400w / medium 800w / full 2000w) for all other kinds.
- **New: Server-side aspect ratio validation** — Logo enforced 1:1 ±10%, Banner 16:9 ±15%, Gallery/Aerial/Approach require minimum 800px wide.
- **New: Photo management API** — upload-ticket, confirm, reorder (swap by ID), and delete endpoints. Logo and Banner are capped at one each per marina.
- **New: Marina operator photo UI** — logo/banner uploaders with crop UI, gallery/aerial/approach management with up/down reorder buttons.
- **New: Onboarding wizard Step 5 "Photos"** — inserted between existing Step 4 (Preview & Adjust) and Step 5 (Review & Publish, which becomes Step 6). Optional with encouragement; focuses on logo and banner.
- **Extended: Marina search response** — embeds `logoUrl` and `bannerThumbnailUrl` on marina search results and marina detail responses.
- **Extended: Demo seed** — Picsum placeholder photo records (deterministic seed-based URLs) added to `DemoSeedScript` for all five photo kinds.

## Capabilities

### New Capabilities

- `storage-provider`: Pluggable file storage abstraction — `IStorageProvider`, `S3StorageProvider`, `FilesystemStorageProvider`, presigned upload ticket flow, variant generation job, storage key conventions, appsettings config.
- `marina-photos`: Marina-level photo management — `MarinaPhoto` entity and EF config, photo kinds (Logo/Banner/Gallery/Aerial/Approach), upload/confirm/reorder/delete API, aspect ratio validation, marina operator gallery UI, logo/banner embedded in search responses, wizard step.

### Modified Capabilities

- `marina-onboarding-wizard`: Adds Step 5 "Photos" to the existing wizard flow (logo + banner upload, optional); existing Step 5 Publish becomes Step 6. `Marina.SetupStep` integer semantics shift by one for new setups.

## Impact

- **Domain**: New `MarinaPhoto` entity in `MyMarina.Domain`.
- **Infrastructure**: New `MyMarina.Infrastructure/Storage/` module — `IStorageProvider`, both implementations, `UploadTicket` record, `ImageVariantGenerationJob` (Hangfire + ImageSharp). EF Core configuration and migration for `MarinaPhoto`. `DemoSeedScript` updated.
- **API**: New `PhotosController` — `POST /marinas/{id}/photos/ticket`, `POST /marinas/{id}/photos/confirm`, `PATCH /marinas/{id}/photos/{photoId}/reorder`, `DELETE /marinas/{id}/photos/{photoId}`. Marina search/detail DTOs gain `logoUrl` + `bannerThumbnailUrl`. OpenAPI spec regeneration required.
- **Frontend**: New photo upload components (crop UI, gallery grid, reorder buttons). Wizard gains a step. Marina search cards and detail page consume new URL fields.
- **Dependencies**: `SixLabors.ImageSharp` NuGet; `AWSSDK.S3` NuGet (already present or new); `react-image-crop` npm package.
- **docker-compose**: MinIO service added.
- **Config**: New `"Storage"` section in `appsettings.json` / `appsettings.Development.json`.
