## ADDED Requirements

### Requirement: Announcements screen lifted from mega-page
`/marina/:marinaId/announcements` SHALL render the announcements panel lifted from `MarinaDashboardPage`. URL params: `status` (default: `all`; values: `all | published | draft`), `id` (selected announcement for edit). Pinned announcements SHALL always appear at the top. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-marina-setup.md#announcements`.

#### Scenario: Published filter shows only published announcements
- **WHEN** `?status=published` is in the URL
- **THEN** only published announcements are shown

#### Scenario: Pinned announcements appear first regardless of filter
- **WHEN** there are both pinned and unpinned announcements
- **THEN** pinned items appear at the top of the list

### Requirement: New announcement action in PageHeader
A single "+ New announcement" primary button SHALL appear in `PageHeader` and open the announcement creation form (lifted from the existing mega-page inline form into a Radix `<Dialog>`).

#### Scenario: Creating an announcement closes the dialog and updates the list
- **WHEN** an operator saves a new announcement
- **THEN** `createAnnouncement` is called, the dialog closes, and the new draft appears in the list
