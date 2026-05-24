## MODIFIED Requirements

### Requirement: Publish step and marina activation
The wizard SHALL have six steps. The final wizard step (Step 6) SHALL display a summary of the marina configuration (name, type, dock count, slip count). The step SHALL include an "List on the marketplace" toggle defaulting to OFF. Educational copy SHALL explain that enabling the toggle means boaters can immediately attempt to book slips. On submit, the system SHALL set `Marina.IsSetupComplete = true` and `Marina.IsListed` per the toggle value.

#### Scenario: Default publish state
- **WHEN** the user reaches the publish step (Step 6)
- **THEN** the "List on marketplace" toggle SHALL be OFF by default

#### Scenario: Marina activated without listing
- **WHEN** the user submits the publish step with the toggle OFF
- **THEN** `Marina.IsSetupComplete` SHALL be set to `true` and `Marina.IsListed` SHALL be `false`
- **THEN** the user SHALL be redirected to `/marina/{id}` (the marina dashboard)

#### Scenario: Marina activated and listed
- **WHEN** the user submits the publish step with the toggle ON
- **THEN** `Marina.IsSetupComplete` SHALL be set to `true` and `Marina.IsListed` SHALL be `true`
- **THEN** the marina's slips SHALL become eligible to appear in marketplace search results

## ADDED Requirements

### Requirement: Wizard photos step
The wizard SHALL include a Step 5 "Photos" step inserted between the existing Step 4 (Preview & Adjust) and the Publish step (now Step 6). The Photos step SHALL present a Logo upload slot and a Banner upload slot as the primary actions, with a muted "Skip for now — add photos later" link at the bottom. A progress indicator SHALL show "Step 5 of 6". The step SHALL NOT be required — clicking skip SHALL advance `Marina.SetupStep` to 6 and proceed to the Publish step immediately. If the operator uploads a logo or banner during the wizard, the standard presigned upload flow SHALL be used. On skip or completion of the step, the system SHALL persist `Marina.SetupStep = 5` (or 6 if skipping directly to Publish).

#### Scenario: Photos step appears between Preview and Publish
- **WHEN** the user advances from Step 4 (Preview & Adjust)
- **THEN** the wizard SHALL display Step 5 "Photos" before the Publish step

#### Scenario: Skip proceeds without requiring upload
- **WHEN** the user clicks "Skip for now" on the Photos step
- **THEN** the wizard SHALL advance to the Publish step without requiring any photo to be uploaded
- **THEN** `Marina.SetupStep` SHALL be set to 6

#### Scenario: Logo uploaded during wizard
- **WHEN** the user uploads a logo on the Photos step
- **THEN** the standard presigned upload and confirm flow SHALL execute
- **THEN** on step completion, the wizard SHALL advance to the Publish step

#### Scenario: Encouragement copy present
- **WHEN** the Photos step is rendered
- **THEN** the step SHALL include motivational copy explaining that marinas with photos receive more inquiries
- **THEN** the "Skip for now" link SHALL be visually de-emphasized relative to the primary "Next" action
