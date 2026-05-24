## REMOVED Requirements

### Requirement: Upload ticket endpoint
**Reason**: Replaced by the single multipart upload endpoint (`marina-photo-upload` capability). The presigned URL pattern was removed due to a security gap (client-reported file size is untrustworthy) and unnecessary CORS configuration on S3.
**Migration**: Replace `POST /marinas/{id}/photos/ticket` + `POST /marinas/{id}/photos/confirm` calls with a single `POST /marinas/{id}/photos` multipart request.

### Requirement: Upload confirm endpoint
**Reason**: Replaced by the single multipart upload endpoint. Confirmation is no longer a separate step — the API controls the full upload lifecycle.
**Migration**: See above.

## MODIFIED Requirements

### Requirement: Marina operator photo management UI
The marina settings (or dedicated photos page) SHALL provide: (1) a Logo upload slot with a 1:1 crop modal; (2) a Banner upload slot with a 16:9 crop modal; (3) a Gallery tab with an upload button, photo grid, up/down reorder buttons, and a delete button per photo; (4) an Aerial tab with the same controls as Gallery; (5) an Approach tab where each photo also has a caption input and optional lat/lng fields. All upload slots SHALL use `react-image-crop` (or equivalent) to enforce the per-kind aspect ratio in the browser before uploading. The `usePhotoUpload` hook SHALL send a single `multipart/form-data` POST to `POST /marinas/{id}/photos` — the two-step ticket/confirm flow is removed.

#### Scenario: Logo upload enforces 1:1 crop
- **WHEN** a marina operator opens the logo upload slot and selects an image
- **THEN** the crop UI SHALL lock the aspect ratio to 1:1 and prevent free-form cropping

#### Scenario: Gallery photo reordered via buttons
- **WHEN** a marina operator clicks the "Move down" button on a gallery photo
- **THEN** that photo SHALL visually move one position down in the grid
- **THEN** the reorder API endpoint SHALL be called and the new order persisted

#### Scenario: Photo shows loading state until variants ready
- **WHEN** a photo has been uploaded but the variant job has not yet completed
- **THEN** the photo slot SHALL render a skeleton/loading indicator instead of an image

#### Scenario: Upload sends a single multipart request
- **WHEN** a marina operator selects and crops an image for upload
- **THEN** the frontend SHALL send one `POST /marinas/{id}/photos` multipart/form-data request containing the cropped file and kind
- **THEN** no separate ticket or confirm request SHALL be made
