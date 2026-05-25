## ADDED Requirements

### Requirement: MarinaPhoto entity
The system SHALL have a `MarinaPhoto` entity with the following fields: `Id` (UUID v7), `TenantId` (for global query filter), `MarinaId`, `Kind` (enum: `Logo=0`, `Banner=1`, `Gallery=2`, `Aerial=3`, `Approach=4`), `StorageKey` (canonical object key), `UrlFull`, `UrlMedium`, `UrlThumbnail` (nullable until variant job completes), `SortOrder` (int), `Width`, `Height`, `FileSizeBytes`, `Caption` (nullable, Approach only), `Latitude` (nullable decimal, Approach only), `Longitude` (nullable decimal, Approach only), `UploadedByUserId`, `UploadedAt`. The EF configuration SHALL enforce a unique filtered index on `(MarinaId, Kind)` covering only `Kind IN (Logo, Banner)` to prevent duplicate logo or banner per marina at the database level.

#### Scenario: Logo uniqueness enforced at DB level
- **WHEN** a second `MarinaPhoto` with `Kind = Logo` is inserted for the same marina
- **THEN** the database SHALL raise a unique constraint violation

#### Scenario: Gallery allows multiple photos per marina
- **WHEN** multiple `MarinaPhoto` records with `Kind = Gallery` are inserted for the same marina
- **THEN** all records SHALL be accepted without constraint violation

### Requirement: Upload ticket endpoint
`POST /marinas/{id}/photos/ticket` SHALL accept `{ kind, contentType, fileSizeBytes, imageWidth?, imageHeight? }` and return an `UploadTicket` from `IStorageProvider`. The handler SHALL validate: (1) the requesting user holds a `Membership` at the marina (any role); (2) `kind` is a valid `MarinaPhotoKind`; (3) `fileSizeBytes` does not exceed 20 MB; (4) if `imageWidth` and `imageHeight` are provided, aspect ratio rules pass for the given kind. Aspect ratio rules: Logo — `|width/height − 1.0| < 0.10`; Banner — `|width/height − 1.778| < 0.267`; Gallery/Aerial/Approach — `width >= 800`. The handler SHALL return 422 with a descriptive error on any validation failure before issuing the presigned URL.

#### Scenario: Valid ticket issued for gallery photo
- **WHEN** a marina manager posts a ticket request for `kind = Gallery` with a 1200×900 image
- **THEN** the API SHALL return 200 with an `UploadTicket` containing a valid upload URL

#### Scenario: Logo aspect ratio rejected
- **WHEN** a ticket request is submitted for `kind = Logo` with `imageWidth = 800` and `imageHeight = 600` (ratio 1.33, exceeds ±10% of 1:1)
- **THEN** the API SHALL return 422 with a message indicating the required aspect ratio

#### Scenario: File size limit enforced
- **WHEN** a ticket request specifies `fileSizeBytes` exceeding 20 MB
- **THEN** the API SHALL return 422 with a message indicating the 20 MB limit

#### Scenario: Non-member rejected
- **WHEN** a user without a `Membership` at the marina (e.g., a boater) calls the ticket endpoint
- **THEN** the API SHALL return 403

#### Scenario: Duplicate logo ticket rejected
- **WHEN** the marina already has a `MarinaPhoto` with `Kind = Logo` and a new ticket for `Kind = Logo` is requested
- **THEN** the API SHALL return 409 indicating a logo already exists (caller must delete existing first)

### Requirement: Upload confirm endpoint
`POST /marinas/{id}/photos/confirm` SHALL accept `{ key, kind, caption?, latitude?, longitude? }` and: (1) call `IStorageProvider.ConfirmUploadAsync(key)` to verify the object exists; (2) perform server-side aspect ratio validation using actual dimensions read from the uploaded image (via ImageSharp metadata); (3) create a `MarinaPhoto` record with `UrlFull/Medium/Thumbnail = null`; (4) enqueue `ImageVariantGenerationJob`; (5) return the created `MarinaPhotoDto`. `caption`, `latitude`, and `longitude` SHALL only be persisted when `kind = Approach`; they SHALL be silently ignored for other kinds.

#### Scenario: Confirm creates photo record and enqueues job
- **WHEN** a client calls confirm with a key for an object that exists in storage
- **THEN** a `MarinaPhoto` record SHALL be created with `UrlFull = null`
- **THEN** `ImageVariantGenerationJob` SHALL be enqueued in Hangfire
- **THEN** the API SHALL return the photo DTO with null variant URLs

#### Scenario: Confirm fails for missing object
- **WHEN** confirm is called with a key for which no object exists in storage
- **THEN** the API SHALL return 404

#### Scenario: Approach photo caption and GPS persisted
- **WHEN** confirm is called with `kind = Approach`, `caption = "Red channel marker"`, `latitude = 27.4`, `longitude = -82.5`
- **THEN** the created `MarinaPhoto` SHALL have the caption and coordinates populated

#### Scenario: Caption ignored for non-Approach kind
- **WHEN** confirm is called with `kind = Gallery` and a `caption` value
- **THEN** the `MarinaPhoto.Caption` SHALL remain null

### Requirement: Photo list endpoint
`GET /marinas/{id}/photos` SHALL return all `MarinaPhoto` records for the marina, ordered by `Kind` then `SortOrder`. Any authenticated user with marina access or any public viewer of a listed marina SHALL be able to read the photo list. Variant URLs that are still null (variant job not yet complete) SHALL be returned as null; clients SHALL render a loading state for those records.

#### Scenario: Photos returned in kind and sort order
- **WHEN** a marina has a Logo, two Gallery photos (SortOrder 0 and 1), and an Approach photo
- **THEN** `GET /marinas/{id}/photos` SHALL return them ordered: Logo, Gallery(0), Gallery(1), Approach

#### Scenario: Null variant URLs included in response
- **WHEN** a photo was just confirmed and the variant job has not yet run
- **THEN** the photo SHALL appear in the list with `urlThumbnail = null`

### Requirement: Photo reorder endpoint
`PATCH /marinas/{id}/photos/{photoId}/reorder` SHALL accept `{ direction: "up" | "down" }`. The handler SHALL locate the adjacent photo within the same `Kind` (next lower or higher `SortOrder`) and swap the two `SortOrder` values in a single transaction. If the photo is already first (up) or last (down) within its kind, the API SHALL return 400. Authorization: same as upload — any Membership at the marina.

#### Scenario: Photo moved up
- **WHEN** a gallery photo at SortOrder 2 is reordered "up"
- **THEN** it SHALL swap SortOrder with the gallery photo at SortOrder 1

#### Scenario: First photo cannot move up
- **WHEN** the photo already has the lowest SortOrder in its kind
- **THEN** the API SHALL return 400

#### Scenario: Last photo cannot move down
- **WHEN** the photo already has the highest SortOrder in its kind
- **THEN** the API SHALL return 400

### Requirement: Photo delete endpoint
`DELETE /marinas/{id}/photos/{photoId}` SHALL: (1) delete the `MarinaPhoto` record; (2) enqueue a Hangfire job to delete the storage object and all its variants (`{key}_thumb`, `{key}_medium`, `{key}_full`). Deletion SHALL NOT be synchronous in the DB transaction. Authorization: any Membership at the marina. Platform operators SHALL also be able to delete any photo.

#### Scenario: Photo deleted and storage cleanup enqueued
- **WHEN** a marina manager deletes a photo
- **THEN** the `MarinaPhoto` record SHALL be removed from the database
- **THEN** a storage cleanup job SHALL be enqueued in Hangfire to delete the original key and all variant keys

#### Scenario: Platform operator can delete any marina's photo
- **WHEN** a platform operator calls the delete endpoint for any marina
- **THEN** the deletion SHALL succeed regardless of marina membership

### Requirement: Logo and banner in marina search and detail responses
The marina search result DTO and marina detail DTO SHALL include `logoUrl` (the Logo photo's `UrlThumbnail`, 256px variant) and `bannerThumbnailUrl` (the Banner photo's `UrlMedium`, 800w variant). Both fields SHALL be `null` when no logo or banner exists.

#### Scenario: Marina search result includes logo URL
- **WHEN** a marina has a Logo photo with a completed variant job
- **THEN** the marina's entry in search results SHALL include a non-null `logoUrl`

#### Scenario: Marina with no logo returns null logoUrl
- **WHEN** a marina has no Logo photo
- **THEN** `logoUrl` SHALL be `null` in search and detail responses

### Requirement: Approach photos shown to boaters
The marina detail page SHALL display Approach photos in a dedicated "Getting Here" section. Approach photos SHALL be shown in `SequenceOrder` (their `SortOrder` within the Approach kind). Each Approach photo tile SHALL display the photo and its `Caption` (if set). If a `Latitude`/`Longitude` is present on any Approach photo, the "Getting Here" section SHALL render a Leaflet map with pins for those photos.

#### Scenario: Approach photos with captions displayed
- **WHEN** a marina has two Approach photos, both with captions
- **THEN** the "Getting Here" section SHALL show both photos with their captions in SortOrder sequence

#### Scenario: Approach photo GPS pin shown on map
- **WHEN** at least one Approach photo has a Latitude and Longitude
- **THEN** a Leaflet map SHALL render in the "Getting Here" section with pins at those coordinates

#### Scenario: No Approach photos — section hidden
- **WHEN** a marina has no Approach photos
- **THEN** the "Getting Here" section SHALL not be rendered on the marina detail page

### Requirement: Marina operator photo management UI
The marina settings (or dedicated photos page) SHALL provide: (1) a Logo upload slot with a 1:1 crop modal; (2) a Banner upload slot with a 16:9 crop modal; (3) a Gallery tab with an upload button, photo grid, up/down reorder buttons, and a delete button per photo; (4) an Aerial tab with the same controls as Gallery; (5) an Approach tab where each photo also has a caption input and optional lat/lng fields. All upload slots SHALL use `react-image-crop` (or equivalent) to enforce the per-kind aspect ratio in the browser before issuing the ticket request.

#### Scenario: Logo upload enforces 1:1 crop
- **WHEN** a marina operator opens the logo upload slot and selects an image
- **THEN** the crop UI SHALL lock the aspect ratio to 1:1 and prevent free-form cropping

#### Scenario: Gallery photo reordered via buttons
- **WHEN** a marina operator clicks the "Move down" button on a gallery photo
- **THEN** that photo SHALL visually move one position down in the grid
- **THEN** the reorder API endpoint SHALL be called and the new order persisted

#### Scenario: Photo shows loading state until variants ready
- **WHEN** a photo has been confirmed but the variant job has not yet completed
- **THEN** the photo slot SHALL render a skeleton/loading indicator instead of an image

### Requirement: Demo seed includes marina photos
`DemoSeedScript.SeedAsync` SHALL create `MarinaPhoto` records for the demo marina covering all five kinds (Logo, Banner, Gallery ×3, Aerial ×1, Approach ×2). Photo records SHALL use Picsum deterministic URLs (e.g., `https://picsum.photos/seed/{slug}/{width}/{height}`) for all three variant URL fields, bypassing the storage upload flow. The CI integration test SHALL assert at least one `MarinaPhoto` record exists in the demo tenant.

#### Scenario: Demo marina has photos for all kinds
- **WHEN** the demo seed script runs
- **THEN** the demo marina SHALL have at least one photo for each of the five `MarinaPhotoKind` values
- **THEN** all `UrlFull`, `UrlMedium`, `UrlThumbnail` fields SHALL be non-null on seeded records
