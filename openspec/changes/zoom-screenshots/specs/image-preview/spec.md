## ADDED Requirements

### Requirement: Click thumbnail opens modal preview

When a user clicks on a screenshot thumbnail in the marketing site's image gallery, the system SHALL display a full-screen modal overlay with the full-resolution screenshot.

#### Scenario: Thumbnail opens modal

- **WHEN** the user clicks on any screenshot thumbnail in the grid
- **THEN** a modal overlay appears with the full-resolution image centered on screen
- **AND** the screenshot caption is displayed below the image inside the modal
- **AND** the remaining screen area is dimmed with a semi-transparent backdrop
- **AND** the user cannot interact with the content behind the modal

#### Scenario: Modal closes on backdrop click

- **WHEN** the modal is open and the user clicks outside the image card (on the dimmed backdrop)
- **THEN** the modal closes
- **AND** the last-clicked screenshot thumbnail remains visually highlighted

#### Scenario: Modal closes on Escape key

- **WHEN** the modal is open and the user presses Escape
- **THEN** the modal closes
- **AND** focus returns to the thumbnail that was clicked to open the modal

#### Scenario: Image displays at readable size

- **WHEN** the modal is open
- **THEN** the image is displayed at its natural resolution
- **AND** the image is constrained to a maximum height of 85% of the viewport height
- **AND** the image is centered both horizontally and vertically within the modal

### Requirement: Prev/next navigation in modal

When the modal is open, the user SHALL be able to navigate between screenshots using left and right arrow buttons.

#### Scenario: Navigate to previous screenshot

- **WHEN** the modal is open and the user clicks the left arrow button
- **THEN** the displayed image changes to the previous screenshot in the grid order
- **AND** the caption below the image updates to match the new screenshot
- **AND** the left arrow button is hidden when viewing the first screenshot

#### Scenario: Navigate to next screenshot

- **WHEN** the modal is open and the user clicks the right arrow button
- **THEN** the displayed image changes to the next screenshot in the grid order
- **AND** the caption below the image updates to match the new screenshot
- **AND** the right arrow button is hidden when viewing the last screenshot

#### Scenario: Wrapping navigation

- **WHEN** the user clicks the right arrow while viewing the last screenshot
- **THEN** the first screenshot is displayed
- **AND** when the user clicks the left arrow while viewing the first screenshot
- **THEN** the last screenshot is displayed

#### Scenario: Keyboard arrow navigation

- **WHEN** the modal is open and the user presses the left arrow key on the keyboard
- **THEN** the previous screenshot is displayed (as if the left arrow button was clicked)
- **AND** when the user presses the right arrow key on the keyboard
- **THEN** the next screenshot is displayed (as if the right arrow button was clicked)

### Requirement: Caption displayed in modal

Each screenshot displayed in the modal SHALL show its associated caption.

#### Scenario: Caption shown below image

- **WHEN** a screenshot is displayed in the modal
- **THEN** the caption (e.g., "Operator Dashboard", "Customer Portal") is displayed below the image inside the modal card
- **AND** the caption uses medium font weight and centered text alignment
- **AND** the caption uses a contrasting text color against the dark modal background

#### Scenario: Caption updates on navigation

- **WHEN** the user navigates between screenshots using prev/next arrows
- **THEN** the caption updates to reflect the currently displayed screenshot
- **AND** the caption transition is instant (no animation delay)

### Requirement: Close button in modal

The modal SHALL include an obvious close button.

#### Scenario: Close button visible

- **WHEN** the modal is open
- **THEN** a close button (×) is visible in the top-right corner of the modal card
- **AND** the close button has a cursor pointer style
- **AND** the close button is visible and accessible on all screen sizes

#### Scenario: Close button functionality

- **WHEN** the user clicks the close (×) button
- **THEN** the modal closes
- **AND** the user returns to the grid view with the last-clicked thumbnail highlighted
