# MyMarina — Project Overview

## Vision

MyMarina is a SaaS marina management platform that gives marinas of all sizes the tools to run their operations — slip management, billing, customer communication, maintenance tracking — through a single, modern web application.

**Domain:** mymarina.org  
**Trademark status:** USPTO application dead/refused/dismissed — name is clear to use.  
**Model:** SaaS (cloud-hosted, multi-tenant). On-premise/self-hosted variant is a future consideration; feature gating via subscription tiers will be designed in from the start.

---

## The Three Personas

### 1. Platform Operators
Anthropic/internal staff who operate the MyMarina SaaS product itself.

- Create and manage marina tenant accounts
- Handle access/support escalations (with full audit trail)
- Manage subscription tiers and feature flags
- Monitor system health

### 2. Marina Operators
The business owners and staff of a marina. This is the primary persona the core data model is built around.

- Configure docks, slips, pricing
- Manage customer accounts and boat registrations
- Handle reservations, seasonal leases, transient stays, waitlists
- Issue invoices, record payments
- Manage maintenance work orders
- Post announcements and news to customers
- Run reports (occupancy, revenue, receivables)
- Manage marina staff accounts with scoped roles

### 3. Marina Customers
Boat owners who are customers of one or more marinas.

- View their slip assignment and lease details
- View and pay invoices
- Register boats and upload documents
- Submit and track maintenance requests
- Read marina announcements
- Manage notification preferences

A customer can have relationships with **multiple marinas** simultaneously.

---

## MVP Scope

The MVP focuses on getting marina operators productive and giving their customers a useful self-service portal.

**In MVP:**
- Marina setup (docks, slips, pricing)
- Customer and boat registration
- Slip assignment / lease management
- Manual invoice creation and payment recording
- Customer portal: view slip, invoices, submit maintenance requests
- Marina announcements / news feed
- Platform operator tenant provisioning

**Post-MVP:**
- Integrated payment processing (abstracted provider interface designed in from day one)
- Reporting and analytics dashboards
- Notification engine (email/SMS)
- Waitlist management
- Supplies and inventory tracking
- Mobile application (API-first design enables this)
- Subdomain-per-tenant routing
- On-premise / self-hosted packaging

---

## Tenant Routing Strategy

Single domain (`app.mymarina.org`). Tenant is resolved from the authenticated user's JWT claims, not from the URL.

A `ITenantResolver` abstraction is registered from the start so subdomain-based resolution can be added later without application rewrites.

---

## Target Customers (Initial)

Small-to-medium marinas. Initial targets are local marinas who can provide early feedback. The data model and infrastructure are designed to scale to large commercial operations without rearchitecting.
