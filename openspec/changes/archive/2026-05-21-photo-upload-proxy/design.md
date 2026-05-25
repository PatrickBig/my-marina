## Context

The current photo upload flow is a two-step presigned URL pattern: the client requests a ticket from the API, gets a presigned S3 PUT URL, uploads directly to S3, then calls a confirm endpoint. This design was chosen to keep large files off the API server, but it introduces three problems:

1. **Security gap**: The ticket validates the client-reported file size, not the actual file size. A malicious client can request a ticket for 1 KB and upload 20 GB to the presigned URL — S3 accepts it, the file lands before the confirm step runs.
2. **CORS requirement**: Browsers making cross-origin PUT requests to S3 require CORS headers configured on the bucket. For RustFS (local dev) this means extra configuration; for Cloudflare R2 production this is a per-bucket setting that must be maintained.
3. **PublicEndpoint hack**: The API communicates with S3 via an internal address (Docker service name), but presigned URLs must contain a browser-reachable address. The current code rewrites the URL origin at runtime — a fragile workaround that adds config complexity and a subtle bug surface.

For a marina SaaS with thousands of operators uploading a handful of photos each, the scale argument for presigned URLs is not valid. Routing uploads through the API is not a bottleneck.

## Goals / Non-Goals

**Goals:**
- Single multipart POST endpoint replaces the two-step ticket + confirm flow
- Real file size enforcement at the API before data reaches S3
- Eliminate CORS configuration requirement on S3
- Eliminate `PublicEndpoint` and `UploadTtlMinutes` from all config and infrastructure
- Simplify `IStorageProvider` interface (remove presigned URL primitives)
- Keep all downstream behavior identical: same storage keys, same `ImageVariantGenerationJob`, same variant URLs, same `MarinaPhoto` entity

**Non-Goals:**
- Changing the variant generation pipeline (Hangfire + SkiaSharp — unchanged)
- Changing photo kinds, sort order, reorder/delete endpoints
- Adding resumable or chunked uploads
- Progress reporting to the browser during upload

## Decisions

### Decision 1 — Multipart/form-data over raw body stream

The new endpoint accepts `multipart/form-data` with fields: `file` (the image binary), `kind`, and optional `caption`, `latitude`, `longitude`. Multipart is the natural browser file upload format, works with `<input type="file">` and `FormData` without custom headers, and allows metadata alongside the binary in one request. A raw body approach would require metadata in query params or a JSON preamble — more complex with no benefit.

### Decision 2 — Size enforcement via ASP.NET Core form limits + explicit check

`[RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]` on the controller action instructs ASP.NET Core's multipart middleware to reject requests where the file part exceeds the limit — the file never fully buffers if it's too large. The action additionally checks `file.Length` explicitly to return a clean `413 Payload Too Large` rather than letting the middleware's exception propagate unformatted.

The `MaxFileSizeBytes` value stays in `StorageOptions.S3` (config key `Storage__S3__MaxFileSizeBytes`) and is injected into the controller. This gives a single source of truth for the limit.

**Alternative considered**: Count bytes in a custom `LimitedStream` wrapper while streaming to S3. Rejected because ASP.NET Core's multipart middleware already buffers form files to disk for large payloads, making the stream-counting approach redundant complexity.

### Decision 3 — Two-pass read of IFormFile (dimension check then upload)

ASP.NET Core's form middleware buffers `IFormFile` to a temp file on disk for large payloads (threshold ~64 KB). This makes the stream seekable: `file.OpenReadStream()` can be called twice. The handler:

1. Opens the stream once to read image dimensions via `SKCodec.Create` — reads only the image header (a few KB for JPEG/PNG), not the full file
2. Validates aspect ratio
3. Opens the stream a second time to upload to S3 via `PutObjectStreamAsync`

This avoids loading the full file into memory. Disk I/O for the second read is minimal since the temp file is typically warm in OS page cache.

**Alternative considered**: Upload to S3 first, then read back to validate dimensions. Rejected because it allows invalid images to land in S3 before rejection, wasting storage and bandwidth.

### Decision 4 — IStorageProvider interface shrinks to five methods

Remove `CreateUploadTicketAsync`, `ConfirmUploadAsync`, `UploadTicket`, and `StoredFileInfo` from `IStorageProvider`. The remaining interface:

```
GetPublicUrl(key) → string
PutObjectStreamAsync(key, stream, contentType)
GetObjectStreamAsync(key) → Stream?
DeleteAsync(key)
DeleteByPrefixAsync(prefix)
```

`PutObjectStreamAsync` already exists and is the correct upload primitive. The S3 provider's presigned URL logic, public-endpoint rewriting, and `GetObjectMetadataAsync` confirm call are all deleted.

### Decision 5 — Config simplification

Remove from `S3Options`: `PublicEndpoint`, `UploadTtlMinutes`. Remove from all environments: docker-compose, `appsettings.Development.json`, Helm values files, GitHub Actions workflow. `BucketPublicBaseUrl` remains — it drives `GetPublicUrl(key)` used for variant URLs served to browsers.

### Decision 6 — Command consolidates upload + record creation

`CreateUploadTicketCommand` and `ConfirmPhotoUploadCommand` are replaced by a single `UploadMarinaPhotoCommand(MarinaId, Kind, Stream, ContentType, FileSizeBytes, Caption?, Latitude?, Longitude?)`. The handler:
- Builds the storage key (same convention as before: `{tenantId}/{marinaId}/marina/{kind}/{photoId}.ext`)
- Calls `PutObjectStreamAsync` to write to S3
- Validates aspect ratio using dimensions from SKCodec (done in controller before calling handler, passed as `Width` / `Height`)
- Creates the `MarinaPhoto` record
- Enqueues `ImageVariantGenerationJob`
- Returns `MarinaPhotoDto`

Aspect ratio validation moves to the controller action (before handler call) since the controller already has the decoded dimensions from the SKCodec pass.

## Risks / Trade-offs

**[Risk] Files now transit the API pod** → The API pod needs sufficient CPU and memory headroom for concurrent uploads. At 20 MB max and a marina SaaS scale (not a consumer photo-sharing app), this is not a concern in practice. Mitigation: document the expected concurrency in the Helm resource limits.

**[Risk] Temp file disk usage on the API pod** → ASP.NET Core buffers form files to disk. Under high concurrent upload load, temp files accumulate. Mitigation: ASP.NET Core cleans them up after the request; Kubernetes ephemeral storage limits provide a safety bound.

**[Risk] Breaking change to frontend** → The ticket + confirm endpoints are removed. The frontend `usePhotoUpload` hook must be updated in the same PR. Mitigation: this is a single hook and the integration tests cover the full flow.

**[Risk] Integration test `InMemoryStorageProvider` needs updating** → The ticket/confirm stubs are no longer needed; the confirm test that pre-populates `InMemoryStorageProvider.Objects` changes to a direct multipart POST. Mitigation: straightforward rewrite; tests become simpler, not more complex.

## Migration Plan

1. Remove `CreateUploadTicketCommand`, `CreateUploadTicketCommandHandler`, `ConfirmPhotoUploadCommand`, `ConfirmPhotoUploadCommandHandler`
2. Add `UploadMarinaPhotoCommand` + handler
3. Strip `IStorageProvider` and `S3StorageProvider` of presigned URL methods
4. Remove `PublicEndpoint` / `UploadTtlMinutes` from `S3Options` and all config files
5. Update `PhotosController` — remove ticket/confirm actions, add multipart upload action
6. Update `InMemoryStorageProvider` in tests — remove ticket/confirm stubs
7. Rewrite `usePhotoUpload.ts` hook on the frontend
8. Update integration tests to POST multipart to the new endpoint
9. No database migration required — `MarinaPhoto` entity is unchanged
10. Rollback: re-add presigned URL flow (all changes are isolated to the photo upload path; no data model impact)
