## ADDED Requirements

### Requirement: Single multipart photo upload endpoint
`POST /marinas/{id}/photos` SHALL accept `multipart/form-data` with fields: `file` (the image binary), `kind` (string matching `MarinaPhotoKind`), and optional `caption`, `latitude`, `longitude` (for Approach photos only). The endpoint SHALL enforce a file size limit equal to `Storage:S3:MaxFileSizeBytes` (default 20 MB) at the ASP.NET Core form layer — payloads exceeding the limit SHALL be rejected before the file is fully buffered. The handler SHALL: (1) decode image dimensions from the uploaded file using `SKCodec` without loading the full image into memory; (2) validate the aspect ratio for the given kind; (3) stream the file to S3 via `IStorageProvider.PutObjectStreamAsync`; (4) create a `MarinaPhoto` record; (5) enqueue `ImageVariantGenerationJob`; (6) return the created `MarinaPhotoDto` with status 201. Authorization: requesting user SHALL hold a `Membership` at the marina (any role).

#### Scenario: Successful gallery photo upload
- **WHEN** a marina manager POSTs a valid JPEG with `kind = Gallery` and a 1200×900 image
- **THEN** the API SHALL return 201 with a `MarinaPhotoDto` containing the photo's ID and null variant URLs
- **THEN** an `ImageVariantGenerationJob` SHALL be enqueued in Hangfire

#### Scenario: File size limit enforced
- **WHEN** a file larger than `MaxFileSizeBytes` is included in the multipart body
- **THEN** the API SHALL return 413 before the file is fully received

#### Scenario: Aspect ratio invalid
- **WHEN** a logo image with a non-square aspect ratio (e.g., 800×600) is uploaded
- **THEN** the API SHALL return 422 with a message describing the required aspect ratio

#### Scenario: Duplicate logo rejected
- **WHEN** the marina already has a `MarinaPhoto` with `Kind = Logo` and a new logo upload is attempted
- **THEN** the API SHALL return 409 indicating a logo already exists

#### Scenario: Non-member rejected
- **WHEN** a user without a `Membership` at the marina calls the upload endpoint
- **THEN** the API SHALL return 403

#### Scenario: Approach photo caption and GPS persisted
- **WHEN** an Approach photo is uploaded with `caption = "Red channel marker"`, `latitude = 27.4`, `longitude = -82.5`
- **THEN** the created `MarinaPhoto` SHALL have the caption and coordinates populated

#### Scenario: Caption ignored for non-Approach kind
- **WHEN** a Gallery photo is uploaded with a `caption` field present
- **THEN** `MarinaPhoto.Caption` SHALL remain null
