## REMOVED Requirements

### Requirement: Presigned upload ticket issuance
**Reason**: The presigned URL upload pattern is removed. File uploads now transit the API, which provides real size enforcement without relying on client-reported values. The `CreateUploadTicketAsync` method and `UploadTicket` / `StoredFileInfo` types are deleted from `IStorageProvider`.
**Migration**: Upload handlers SHALL call `PutObjectStreamAsync` directly with the incoming request stream.

### Requirement: Upload confirmation and object verification
**Reason**: The confirm step is eliminated. The API now controls the full upload lifecycle — `PutObjectStreamAsync` completes before the handler creates the `MarinaPhoto` record, so there is no race condition to verify.
**Migration**: Remove `ConfirmUploadAsync` call sites.

### Requirement: Filesystem provider for NFS and local fallback
**Reason**: Already removed in the `marina-photo-infrastructure` change. All environments use an S3-compatible provider.
**Migration**: Set `Storage:Provider = "S3"` and configure `Storage:S3` credentials for all environments.

### Requirement: MinIO in docker-compose for local development
**Reason**: MinIO licensing changes prompted a switch to RustFS (already completed in `marina-photo-infrastructure`).
**Migration**: Use RustFS at port 9000. Configuration and docker-compose service already updated.

## MODIFIED Requirements

### Requirement: Pluggable storage provider abstraction
The system SHALL provide an `IStorageProvider` interface in `MyMarina.Infrastructure` that abstracts file storage operations. The active implementation SHALL be selected at startup via `appsettings.json → Storage:Provider` (`"S3"` is the only supported value). Application-layer handlers SHALL depend only on `IStorageProvider` and SHALL NOT reference any vendor-specific storage SDK directly. The interface SHALL expose exactly five methods: `PutObjectStreamAsync`, `GetObjectStreamAsync`, `GetPublicUrl`, `DeleteAsync`, `DeleteByPrefixAsync`.

#### Scenario: Provider selected via configuration
- **WHEN** `Storage:Provider` is set to `"S3"` in configuration
- **THEN** `S3StorageProvider` SHALL be registered as `IStorageProvider`

#### Scenario: Missing or unrecognised provider fails fast
- **WHEN** `Storage:Provider` is missing or set to an unrecognised value at application startup
- **THEN** the application SHALL throw a configuration exception and refuse to start

### Requirement: S3-compatible provider (R2, RustFS, AWS)
The `S3StorageProvider` SHALL use `AWSSDK.S3` with a configurable `ServiceURL` endpoint, enabling it to target Cloudflare R2, RustFS, and AWS S3 from the same implementation. Required configuration: `Storage:S3:Endpoint`, `Storage:S3:Bucket`, `Storage:S3:AccessKey`, `Storage:S3:SecretKey`, `Storage:S3:BucketPublicBaseUrl`. The `PublicEndpoint` and `UploadTtlMinutes` configuration keys are removed.

#### Scenario: RustFS used in local development
- **WHEN** `Storage:S3:Endpoint` is set to `http://rustfs:9000` (internal Docker address) and `Storage:S3:BucketPublicBaseUrl` is set to `http://localhost:9000/mymarina-local`
- **THEN** `S3StorageProvider` SHALL operate identically to the production R2 configuration
- **THEN** photo variant URLs returned by `GetPublicUrl` SHALL resolve against `localhost:9000`

#### Scenario: Public URL uses BucketPublicBaseUrl
- **WHEN** `S3StorageProvider.GetPublicUrl` is called with a storage key
- **THEN** the returned URL SHALL be `{BucketPublicBaseUrl}/{key}` with no runtime URL rewriting
