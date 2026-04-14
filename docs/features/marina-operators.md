# Feature Area: Marina Operators

Marina operators are the business owners and staff who manage a specific marina. This is the primary persona the core data model is built around.

---

## Marina Setup & Configuration

**Goal:** Allow a marina to configure their physical layout and profile.

| Feature | Description | MVP |
|---|---|---|
| Marina profile | Name, address, contact info, description, timezone, hours | Yes |
| Dock management | Create/edit/delete docks; set sort order | Yes |
| Slip management | Add slips to docks; set size constraints, type, rates, amenities | Yes |
| Slip status management | Mark slips as available, under maintenance, inactive | Yes |
| Slip map / occupancy view | Visual grid of docks and slips with status/assignment at a glance | No (post-MVP) |

---

## Customer Management

**Goal:** Manage the marina's customer base.

| Feature | Description | MVP |
|---|---|---|
| Customer list | Search, filter, paginate customers | Yes |
| Customer profile | View contact info, boats, current slip, invoice history, requests | Yes |
| Create customer | Manually add a new customer and invite them to create an account | Yes |
| Edit customer | Update contact info, notes | Yes |
| Deactivate customer | Soft-remove without deleting history | Yes |
| Merge customers | Handle duplicate accounts | No |

---

## Boat Registration

**Goal:** Track customer vessels, which determines slip eligibility.

| Feature | Description | MVP |
|---|---|---|
| Register boat | Add make, model, year, length, beam, draft, type, registration #, insurance | Yes |
| Edit boat | Update vessel details | Yes |
| Remove boat | Soft-delete; retain on historical assignments | Yes |
| Boat document upload | Attach registration and insurance documents | No (post-MVP) |
| Insurance expiry alerts | Warn operator when insurance is expiring | No |

---

## Slip Assignment & Leases

**Goal:** Assign slips to customers for defined periods.

| Feature | Description | MVP |
|---|---|---|
| Assign slip | Link a slip to a customer and boat for a date range | Yes |
| Assignment types | Seasonal, Annual, Monthly, Transient | Yes |
| Rate override | Override the slip's standard rate for a specific assignment | Yes |
| End/terminate assignment | Close out a slip assignment | Yes |
| Slip availability check | See which slips can fit a given boat before assigning | Yes |
| Conflict detection | Prevent double-booking a slip | Yes |
| Waitlist management | Queue customers for a slip type or specific slip | No |

---

## Billing & Invoicing

**Goal:** Track what customers owe and what has been paid.

| Feature | Description | MVP |
|---|---|---|
| Create invoice | Manual invoice with line items; link to slip assignment | Yes |
| Invoice statuses | Draft → Sent → Paid / Overdue / Voided | Yes |
| Add line items | Custom line items (slip fees, fuel, service charges, etc.) | Yes |
| Record payment | Manual payment recording (cash, check, card, etc.) | Yes |
| Partial payments | Record multiple payments against one invoice | Yes |
| Void invoice | Cancel an invoice with a reason | Yes |
| Overdue detection | Auto-flag invoices past their due date | Yes |
| Send invoice to customer | Email invoice PDF to customer | No (post-MVP) |
| Late fee application | Manually add a late fee line item | Yes |
| Late fee automation | Auto-apply configurable late fees on overdue invoices | No |
| Recurring invoices | Auto-generate monthly/seasonal invoices | No |
| Payment provider integration | Stripe or similar — customer pays online | No |
| Tax configuration | Set tax rate(s) per marina | No |
| Invoice PDF generation | Printable/downloadable invoice | No |

---

## Maintenance & Work Orders

**Goal:** Track work to be done at the marina, whether internally scheduled or customer-requested.

| Feature | Description | MVP |
|---|---|---|
| View maintenance requests | See all customer-submitted requests with status | Yes |
| Update request status | Move through Submitted → Under Review → In Progress → Completed / Declined | Yes |
| Create work order | Create an internal work order from scratch or from a customer request | Yes |
| Assign work order | Assign to a staff member | Yes |
| Work order status | Track Open → In Progress → On Hold → Completed | Yes |
| Schedule work order | Set a scheduled date | Yes |
| Add completion notes | Record what was done | Yes |

---

## Announcements & Communication

**Goal:** Keep customers informed.

| Feature | Description | MVP |
|---|---|---|
| Create announcement | Post news, alerts, or updates for customers | Yes |
| Draft / publish | Save as draft before publishing | Yes |
| Pin announcement | Keep important announcements at the top | Yes |
| Expire announcement | Auto-hide after a specified date | Yes |
| Edit / delete announcement | Manage existing posts | Yes |
| Targeted announcements | Send to a specific dock, slip, or customer segment | No |
| Email blast | Send announcement via email to all customers | No |
| SMS notifications | Text customers about urgent updates | No |

---

## Staff Management

**Goal:** Allow marina owners to manage their employees' access.

| Feature | Description | MVP |
|---|---|---|
| Invite staff | Send invitation by email to create a staff account | Yes |
| Staff roles | MarinaOwner, MarinaStaff (scoped permissions TBD) | Yes |
| Deactivate staff | Revoke access without deleting the account | Yes |
| View staff list | See all staff members and their roles | Yes |
| Granular permissions | e.g., billing-only, maintenance-only staff roles | No |

---

## Reporting & Analytics

**Goal:** Give marina operators visibility into their business performance.

| Feature | Description | MVP |
|---|---|---|
| Occupancy summary | How many slips are occupied vs. available | No |
| Revenue report | Total invoiced and collected by period | No |
| Aging receivables | Outstanding invoices grouped by age | No |
| Customer activity | Recent activity per customer | No |
| Slip utilization | Occupancy rate over time | No |

---

## Notes

- Staff roles inherit TenantId — a staff member can only see data for their marina
- All billing mutations (create invoice, record payment, void) are written to `AuditLog`
- Rate logic (daily, monthly, annual, per-foot vs. flat) lives in the `Slip` entity; `SlipAssignment` can override
