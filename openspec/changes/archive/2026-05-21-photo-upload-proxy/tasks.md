## 1. Remove Presigned URL Infrastructure

- [x] 1.1 Delete `CreateUploadTicketCommand.cs` and `CreateUploadTicketCommandHandler.cs` from `MyMarina.Application` and `MyMarina.Infrastructure`
- [x] 1.2 Delete `ConfirmPhotoUploadCommand.cs` and `ConfirmPhotoUploadCommandHandler.cs` from `MyMarina.Application` and `MyMarina.Infrastructure`
- [x] 1.3 Remove `CreateUploadTicketAsync` and `ConfirmUploadAsync` methods from `IStorageProvider`
- [x] 1.4 Remove `UploadTicket` and `StoredFileInfo` types from `MyMarina.Infrastructure.Storage`
- [x] 1.5 Remove `CreateUploadTicketAsync` and `ConfirmUploadAsync` implementations from `S3StorageProvider`
- [x] 1.6 Remove the presigned URL rewrite logic (`PublicEndpoint`) from `S3StorageProvider`
- [x] 1.7 Remove `PublicEndpoint` and `UploadTtlMinutes` properties from `S3Options`
- [x] 1.8 Remove `Storage__S3__PublicEndpoint` and `Storage__S3__UploadTtlMinutes` from `docker-compose.yml` (api and api-setup services)
- [x] 1.9 Remove `PublicEndpoint` and `UploadTtlMinutes` from `appsettings.Development.json`
- [x] 1.10 Remove `storage.s3.publicEndpoint` and `storage.s3.uploadTtlMinutes` from Helm `values.yaml`, `values-staging.yaml`, `values-prod.yaml`
- [x] 1.11 Remove corresponding env var mappings from Helm `deployment.yaml` and `setup-job.yaml` templates

## 2. Add Multipart Upload Command

- [x] 2.1 Add `UploadMarinaPhotoCommand` record to `MyMarina.Application.Photos` with fields: `MarinaId`, `Kind`, `Stream`, `ContentType`, `FileSizeBytes`, `Width`, `Height`, `Caption?`, `Latitude?`, `Longitude?`
- [x] 2.2 Add `ICommandHandler<UploadMarinaPhotoCommand, MarinaPhotoDto>` registration
- [x] 2.3 Implement `UploadMarinaPhotoCommandHandler` in `MyMarina.Infrastructure.Photos`:
  - Build storage key using the existing key convention (`{tenantId}/{marinaId}/marina/{kind}/{photoId}.ext`)
  - Call `IStorageProvider.PutObjectStreamAsync`
  - Check for duplicate logo/banner (409 if exists)
  - Create `MarinaPhoto` record with next `SortOrder`
  - Enqueue `ImageVariantGenerationJob`
  - Return `MarinaPhotoDto`

## 3. Update PhotosController

- [x] 3.1 Remove `CreateTicket` and `Confirm` actions from `PhotosController`
- [x] 3.2 Remove `ICommandHandler<CreateUploadTicketCommand, UploadTicket>` and `ICommandHandler<ConfirmPhotoUploadCommand, MarinaPhotoDto>` constructor parameters
- [x] 3.3 Add `ICommandHandler<UploadMarinaPhotoCommand, MarinaPhotoDto>` constructor parameter
- [x] 3.4 Add `POST /marinas/{id}/photos` action decorated with `[RequestFormLimits(MultipartBodyLengthLimit = ...)]` bound from config
- [x] 3.5 In the new action: parse `kind` from form field, read `IFormFile`, check `file.Length` against limit and return 413 if exceeded
- [x] 3.6 In the new action: use `SKCodec.Create` on `file.OpenReadStream()` to read `width` and `height`; return 422 if file cannot be decoded
- [x] 3.7 In the new action: call `AspectRatioValidator.Validate` with decoded dimensions; return 422 with error message on failure
- [x] 3.8 In the new action: call `UploadMarinaPhotoCommand` handler with second `file.OpenReadStream()` for the S3 stream; handle 409 (duplicate logo/banner) and 403
- [x] 3.9 Remove `CreateUploadTicketRequest` and `ConfirmUploadRequest` request records from the controller file

## 4. Update InMemoryStorageProvider (Tests)

- [x] 4.1 Remove `CreateUploadTicketAsync` and `ConfirmUploadAsync` stubs from `InMemoryStorageProvider` in `MyMarina.IntegrationTests`
- [x] 4.2 Remove `UploadTicket` and `StoredFileInfo` stub implementations
- [x] 4.3 Ensure `PutObjectStreamAsync` in `InMemoryStorageProvider` reads and stores the stream bytes in `Objects` dictionary (needed for variant job tests)

## 5. Update Integration Tests

- [x] 5.1 Rewrite `PhotosTests.cs` — remove ticket and confirm test helpers; replace with multipart POST helper that submits an `IFormFile`-equivalent multipart body
- [x] 5.2 Add test: `POST /photos` with valid JPEG returns 201 with `MarinaPhotoDto` and enqueues job
- [x] 5.3 Add test: `POST /photos` with non-member returns 403
- [x] 5.4 Add test: `POST /photos` with oversized file returns 413
- [x] 5.5 Add test: `POST /photos` with bad aspect ratio returns 422 (logo with non-square dimensions)
- [x] 5.6 Add test: `POST /photos` duplicate logo returns 409

## 6. Update Frontend Hook

- [x] 6.1 Rewrite `usePhotoUpload.ts` — remove the three-step ticket/upload/confirm flow; replace with a single `FormData` POST to `POST /marinas/{id}/photos`
- [x] 6.2 Remove the `uploadUrl`, `method`, and `requiredHeaders` logic from the hook
- [x] 6.3 Ensure the hook still accepts the cropped `Blob`, `kind`, and optional `caption`/`latitude`/`longitude` and maps them to form fields
- [x] 6.4 Verify that `PhotoUploadSlot.tsx` and all photo tab components work correctly with the simplified hook (no interface changes expected)

## 7. Regenerate API Types and Verify

- [x] 7.1 Start the API and run `npm run generate-api` from `src/MyMarina.Web/` to regenerate `schema.d.ts` reflecting the removed ticket/confirm endpoints and new upload endpoint
- [x] 7.2 Fix any TypeScript errors in the frontend caused by removed types (`UploadTicket`, ticket/confirm response shapes)
- [x] 7.3 Run `dotnet test` — all tests pass
- [x] 7.4 Run `npm run build` from `src/MyMarina.Web/` — no TypeScript errors
- [x] 7.5 Manual smoke test: upload a logo photo end-to-end in the running docker-compose stack; confirm variant URLs populate after the Hangfire job runs
