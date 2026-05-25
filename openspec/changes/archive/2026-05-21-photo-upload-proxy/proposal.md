## Why

The current presigned URL upload flow has a real security gap: file size limits are enforced only against the client-reported size at ticket-request time, so a malicious operator can request a ticket claiming a small file then upload an arbitrarily large file directly to S3. For a marina SaaS with thousands of operators uploading a handful of photos each, the scale justification for presigned URLs doesn't exist — routing uploads through the API is not a bottleneck, and it makes enforcement trivial.

## What Changes

- **BREAKING**: Remove `POST /marinas/{id}/photos/ticket` and `POST /marinas/{id}/photos/confirm` endpoints
- **BREAKING**: Replace with a single `POST /marinas/{id}/photos` multipart endpoint that accepts the file, kind, and optional metadata in one request
- Remove `CreateUploadTicketAsync`, `ConfirmUploadAsync`, `UploadTicket`, and `StoredFileInfo` from `IStorageProvider` — the abstraction only needs `PutObjectStreamAsync`, `GetPublicUrl`, `DeleteAsync`, `DeleteByPrefixAsync`, and `GetObjectStreamAsync`
- Remove `S3StorageProvider` presigned URL logic and the `PublicEndpoint` URL-rewrite hack
- Remove `Storage__S3__PublicEndpoint` and `Storage__S3__UploadTtlMinutes` from all config (appsettings, docker-compose, Helm values, GitHub Actions)
- Enforce file size in real time on the API as bytes stream in — abort and clean up if limit is exceeded before the full file lands in S3
- Remove S3 CORS configuration requirement — browsers no longer talk directly to storage
- Replace the two-step `usePhotoUpload` hook on the frontend with a single multipart POST
- Simplify `InMemoryStorageProvider` in integration tests — no ticket/confirm stubs needed

## Capabilities

### New Capabilities

- `marina-photo-upload`: Operator uploads a marina photo via a single multipart API request; the API streams the file to S3 with real-time size enforcement, validates aspect ratio, creates the `MarinaPhoto` record, and enqueues the variant generation job.

### Modified Capabilities

- `marina-photo-management`: The public contract for uploading photos changes from two-step (ticket + confirm) to one-step (multipart POST). List, reorder, and delete operations are unchanged.

## Impact

- **API**: `PhotosController` — remove ticket/confirm actions, add multipart upload action
- **Application**: `CreateUploadTicketCommand`, `ConfirmPhotoUploadCommand` replaced by `UploadMarinaPhotoCommand`
- **Infrastructure**: `S3StorageProvider` simplified; `IStorageProvider` interface shrinks; `ConfirmPhotoUploadCommandHandler` removed; new `UploadMarinaPhotoCommandHandler`
- **Frontend**: `usePhotoUpload.ts` hook rewritten; `PhotoUploadSlot` component unchanged externally
- **Config**: `Storage__S3__PublicEndpoint` and `Storage__S3__UploadTtlMinutes` removed everywhere
- **Tests**: `InMemoryStorageProvider` simplified; photo upload integration tests rewritten against new endpoint
- **No impact**: `ImageVariantGenerationJob`, `MarinaPhoto` entity, variant URL columns, reorder/delete endpoints, search result photo URLs
