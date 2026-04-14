# Feature Area: Marina Customers

Marina customers are boat owners who use the customer portal to interact with their marina. A single customer user account may have relationships with multiple marinas.

---

## Account & Profile

**Goal:** Let customers manage their own identity and preferences.

| Feature | Description | MVP |
|---|---|---|
| Register account | Sign up via invitation link from marina or self-registration | Yes |
| Edit profile | Update name, phone, email, emergency contact | Yes |
| Change password | Self-service password change | Yes |
| Notification preferences | Opt in/out of email notifications by category | No |
| Multi-marina view | See all marinas they are a customer of from one account | Yes |

---

## My Slip

**Goal:** Give customers visibility into their current berth and lease details.

| Feature | Description | MVP |
|---|---|---|
| View current slip | See slip name, dock, amenities (electric, water) | Yes |
| View assignment details | Lease type, start/end date, assigned boat | Yes |
| View slip history | Past slip assignments | Yes |

---

## Boat Management

**Goal:** Let customers manage their vessel information.

| Feature | Description | MVP |
|---|---|---|
| View registered boats | List all boats on their account | Yes |
| Add a boat | Register a new vessel (marina must approve assignment) | Yes |
| Edit a boat | Update vessel details | Yes |
| Remove a boat | Request removal (subject to no active assignments) | Yes |
| Upload boat documents | Registration certificate, insurance doc | No |

---

## Billing & Payments

**Goal:** Give customers full visibility into their financial relationship with the marina.

| Feature | Description | MVP |
|---|---|---|
| View invoices | List all invoices with status, due date, amount | Yes |
| View invoice detail | Line items, payment history, balance due | Yes |
| Download invoice | PDF of invoice | No |
| Record payment (manual) | Acknowledge a payment was made (marina confirms) | No — marina records payments in MVP |
| Pay online | Integrated payment via Stripe or similar | No |
| Autopay setup | Set up recurring payment method | No |
| Payment history | View all past payments | Yes |

---

## Maintenance Requests

**Goal:** Let customers report problems and track the status of repairs.

| Feature | Description | MVP |
|---|---|---|
| Submit request | Describe an issue; optionally link to a slip or boat | Yes |
| View request status | Track Submitted → Under Review → In Progress → Completed / Declined | Yes |
| View request history | All past requests and their outcomes | Yes |
| Add comment to request | Follow-up information after submission | No |
| Receive status update notifications | Email/SMS when status changes | No |

---

## News & Announcements

**Goal:** Keep customers informed of marina news, alerts, and events.

| Feature | Description | MVP |
|---|---|---|
| View announcement feed | Chronological feed of marina posts, pinned items first | Yes |
| Mark as read | Personal read tracking | No |
| Announcement detail | Full announcement content | Yes |

---

## Notes

- Customers authenticate with the same auth system but hold the `Customer` role
- Their JWT is scoped to their `TenantId` — they cannot see other marinas' data
- A customer with accounts at two marinas will have two separate customer profiles (one per tenant), linked to one user account
- In MVP, customers cannot pay online — they call/visit the marina and the operator records the payment
- The self-registration flow should allow a marina to either (a) invite customers via email or (b) enable open self-registration with marina approval
