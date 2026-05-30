## ADDED Requirements

### Requirement: Photo management accessible from Settings
The photo upload and management flow (CropUploadModal, PhotoCard, usePhotoUpload) SHALL be accessible from the new Settings > Photos tab, in addition to the existing setup wizard entry point. The underlying upload behavior and API calls are unchanged; only a second entry point is added.

#### Scenario: Photos tab renders existing photo grid
- **WHEN** an operator navigates to `/marina/:id/settings?tab=photos`
- **THEN** the photo grid showing all existing marina photos is visible

#### Scenario: Upload from Settings persists photos
- **WHEN** an operator uploads a photo from the Settings Photos tab
- **THEN** the photo is saved via the same photo asset endpoint used by the setup wizard
