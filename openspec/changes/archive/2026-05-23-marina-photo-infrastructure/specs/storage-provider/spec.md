## ADDED Requirements

### Requirement: Pluggable storage provider abstraction
The system SHALL provide an `IStorageProvider` interface in `MyMarina.Infrastructure` that abstracts all file storage operations. The active implementation SHALL be selected at startup via `appsettings.json → Storage:Provider` (`"S3"` or `"Filesystem"`). Application-layer handlers SHALL depend only on `IStorageProvider` and SHALL NOT reference any vendor-specific storage SDK directly.

#### Scenario: Provider selected via configuration
- **WHEN** `Storage:Provider` is set to `"S3"` in configuration
- **THEN** `S3StorageProvider` SHALL be registered as `IStorageProvider`
- **WHEN** `Storage:Provider` is set to `"Filesystem"` in configuration
- **THEN** `FilesystemStorageProvider` SHALL be registered as `IStorageProvider`

#### Scenario: Missing configuration fails fast
- **WHEN** `Storage:Provider` is missing or set to an unrecognised value at application startup
- **THEN** the application SHALL throw a configuration exception and refuse to start

### Requirement: Presigned upload ticket issuance
`IStorageProvider.CreateUploadTicketAsync` SHALL return an `UploadTicket` containing: an `UploadUrl` (presigned PUT URL for S3, API proxy URL for filesystem), an HTTP `Method` (`"PUT"` or `"POST"`), a `RequiredHeaders` dictionary (e.g., `Content-Type`), the storage `Key`, and an `ExpiresAt` timestamp. Ticket lifetime SHALL be configurable (default 15 minutes). Files SHALL be capped at a configurable maximum size (default 20 MB); the handler SHALL reject ticket requests that exceed this limit with 422 before issuing the URL.

#### Scenario: S3 presigned URL issued
- **WHEN** `S3StorageProvider.CreateUploadTicketAsync` is called with a valid key and content type
- **THEN** the returned `UploadUrl` SHALL be a presigned S3/R2 PUT URL valid for the configured TTL
- **THEN** `RequiredHeaders` SHALL include `Content-Type` matching the requested content type

#### Scenario: Filesystem proxy URL issued
- **WHEN** `FilesystemStorageProvider.CreateUploadTicketAsync` is called
- **THEN** the returned `UploadUrl` SHALL be an API-relative path (`/api/photos/local-upload/{token}`)
- **THEN** `Method` SHALL be `"POST"`
- **THEN** the token SHALL be a signed, single-use JWT with the configured TTL

#### Scenario: File size limit enforced at ticket request
- **WHEN** the `fileSizeBytes` supplied in the ticket request exceeds the configured maximum
- **THEN** the API SHALL return 422 with a message indicating the size limit

### Requirement: Upload confirmation and object verification
`IStorageProvider.ConfirmUploadAsync` SHALL verify that the object at the given key exists in storage and SHALL return a `StoredFileInfo` record containing the confirmed file size in bytes. For S3, confirmation is performed by calling `GetObjectMetadataRequest`. For the filesystem provider, the file is checked for existence on disk. If the object does not exist, `ConfirmUploadAsync` SHALL throw an exception that the confirm endpoint maps to 404.

#### Scenario: Confirm succeeds after direct S3 upload
- **WHEN** the client uploads a file directly to the presigned S3 URL and then calls the confirm endpoint
- **THEN** `ConfirmUploadAsync` SHALL return the confirmed file size
- **THEN** the API SHALL create the `MarinaPhoto` record and enqueue the variant generation job

#### Scenario: Confirm fails when object is absent
- **WHEN** the confirm endpoint is called with a key for which no object exists in storage
- **THEN** the API SHALL return 404

### Requirement: Public URL resolution
`IStorageProvider.GetPublicUrl(key)` SHALL return the publicly accessible CDN or base URL for a stored object. For S3: `{BucketPublicBaseUrl}/{key}` (Cloudflare R2 public bucket URL or equivalent). For filesystem: `{BaseUrl}/{key}` as configured in `Storage:Filesystem:BaseUrl`.

#### Scenario: S3 public URL format
- **WHEN** `S3StorageProvider.GetPublicUrl` is called with a storage key
- **THEN** the returned URL SHALL combine the configured `BucketPublicBaseUrl` with the key

#### Scenario: Filesystem public URL format
- **WHEN** `FilesystemStorageProvider.GetPublicUrl` is called with a storage key
- **THEN** the returned URL SHALL combine the configured `BaseUrl` with the key

### Requirement: Object deletion
`IStorageProvider.DeleteAsync(key)` SHALL delete a single object from storage. `IStorageProvider.DeleteByPrefixAsync(prefix)` SHALL delete all objects whose keys begin with the given prefix. Both methods SHALL be idempotent — deleting a non-existent key SHALL NOT throw.

#### Scenario: Single object deleted
- **WHEN** `DeleteAsync` is called for an existing key
- **THEN** the object SHALL be removed from storage

#### Scenario: Prefix delete clears all matching objects
- **WHEN** `DeleteByPrefixAsync` is called with a prefix (e.g., `{tenantId}/{marinaId}/`)
- **THEN** all objects whose keys start with that prefix SHALL be removed

#### Scenario: Delete of non-existent key is safe
- **WHEN** `DeleteAsync` or `DeleteByPrefixAsync` is called for a key that does not exist
- **THEN** no error SHALL be thrown

### Requirement: S3-compatible provider (R2, MinIO, AWS)
The `S3StorageProvider` SHALL use `AWSSDK.S3` with a configurable `ServiceURL` endpoint, enabling it to target Cloudflare R2, MinIO, and AWS S3 from the same implementation. Required configuration: `Storage:S3:Endpoint`, `Storage:S3:Bucket`, `Storage:S3:AccessKey`, `Storage:S3:SecretKey`, `Storage:S3:BucketPublicBaseUrl`.

#### Scenario: MinIO used in local development
- **WHEN** `Storage:S3:Endpoint` is set to `http://localhost:9000` (MinIO)
- **THEN** `S3StorageProvider` SHALL operate identically to the production R2 configuration
- **THEN** presigned PUT URLs SHALL resolve against the MinIO host

### Requirement: Filesystem provider for NFS and local fallback
The `FilesystemStorageProvider` SHALL read and write files to a configurable base path (`Storage:Filesystem:BasePath`). The local upload proxy endpoint (`POST /api/photos/local-upload/{token}`) SHALL stream the request body directly to disk without buffering the full payload in memory. Upload tokens SHALL be single-use JWTs signed with the application's key; the controller SHALL reject replayed tokens.

#### Scenario: File written to configured base path
- **WHEN** a file is uploaded via the filesystem proxy endpoint
- **THEN** the file SHALL be written to `{BasePath}/{key}` on disk

#### Scenario: Replayed upload token rejected
- **WHEN** a previously used upload token is submitted to the proxy endpoint
- **THEN** the endpoint SHALL return 401

### Requirement: MinIO in docker-compose for local development
The `docker-compose.yml` SHALL include a MinIO service with the S3 API exposed on port 9000 and the MinIO console on port 9001. Default credentials SHALL be configured via environment variables. `appsettings.Development.json` SHALL include a `Storage:S3` configuration block pointing to `http://localhost:9000`.

#### Scenario: Local dev stack starts with MinIO
- **WHEN** `docker-compose up` is run
- **THEN** MinIO SHALL start and be accessible at `http://localhost:9000`
- **THEN** the application SHALL connect to MinIO using the configured credentials

### Requirement: Image variant generation
A Hangfire background job (`ImageVariantGenerationJob`) SHALL be enqueued immediately after a `MarinaPhoto` record is confirmed. The job SHALL use ImageSharp (SixLabors) to generate size variants from the original stored object and write variants back to storage under derivative keys. Variant sets: **Square** (64px, 256px, 512px square crops) for `Logo`; **Landscape** (`_thumb` 400px wide, `_medium` 800px wide, `_full` 2000px wide, proportional height) for all other photo kinds. After all variants are written, the job SHALL update `MarinaPhoto.UrlThumbnail`, `UrlMedium`, and `UrlFull` with their CDN URLs. The job SHALL be idempotent — re-running it for an already-processed photo SHALL overwrite variants without error.

#### Scenario: Variants generated after confirm
- **WHEN** a photo is confirmed and the variant generation job runs
- **THEN** variant objects SHALL exist in storage at their expected keys
- **THEN** `MarinaPhoto.UrlThumbnail`, `UrlMedium`, `UrlFull` SHALL be populated with non-null CDN URLs

#### Scenario: Logo uses square variant set
- **WHEN** a photo with `Kind = Logo` is processed
- **THEN** the job SHALL produce 64×64, 256×256, and 512×512 square variants

#### Scenario: Banner uses landscape variant set
- **WHEN** a photo with `Kind = Banner` is processed
- **THEN** the job SHALL produce 400px-wide, 800px-wide, and 2000px-wide variants with proportional height

#### Scenario: Job is idempotent
- **WHEN** the variant generation job is enqueued and run more than once for the same photo
- **THEN** the job SHALL complete without error, overwriting existing variants

### Requirement: Orphan cleanup job
A nightly Hangfire recurring job SHALL scan for `MarinaPhoto` records with `UrlFull = null` older than one hour and delete their storage objects and DB records. This recovers from abandoned uploads where the client issued a ticket but never called confirm, or where the variant job failed permanently.

#### Scenario: Abandoned upload cleaned up
- **WHEN** a `MarinaPhoto` record has `UrlFull = null` and `UploadedAt` is more than 1 hour ago
- **THEN** the nightly job SHALL delete the storage object (if it exists) and the DB record
