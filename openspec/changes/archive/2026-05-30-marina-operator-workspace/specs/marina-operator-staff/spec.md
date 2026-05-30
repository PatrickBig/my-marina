## ADDED Requirements

### Requirement: Staff screen lifted from mega-page
`/marina/:marinaId/staff` SHALL render the staff table lifted from `MarinaDashboardPage`. The table shows: person (avatar + name + email), role badge, scope, last active, and kebab actions (Resend invite / Change role / Revoke). A footnote SHALL indicate that granular per-area permissions are post-MVP. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-marina-setup.md#staff`.

#### Scenario: Staff table shows all memberships for the marina
- **WHEN** a marina has 3 staff members
- **THEN** all 3 appear in the table with their roles

#### Scenario: Unaccepted invites show "Invite pending" badge
- **WHEN** a staff member has not accepted their invitation
- **THEN** their Last active cell shows an "Invite pending" badge

### Requirement: Invite staff action in PageHeader
A single "Invite staff" primary button SHALL appear in `PageHeader` and open an email-invite dialog (lifted from the existing mega-page form).

#### Scenario: Inviting a staff member sends an invite
- **WHEN** an operator enters an email and submits the invite dialog
- **THEN** `inviteStaff` is called and the new pending member appears in the table
