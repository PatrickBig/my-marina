# Open questions

Decisions to confirm with Patrick before or during implementation. None of these
block PR 1.

## Routing

1. **Confirm TanStack Router (code-based) is the right call.** The library is
   installed but unused. An alternative is to stay with the hand-rolled router
   and add a small `useUrlState` of our own. Recommendation in `routing.md` is
   TanStack Router because of typed search params + nested layouts; flag now if
   you'd rather not.

2. **Membership guard placement.** Today the mega-page checks membership at
   mount. Moving it to the workspace layout means every operator route is
   protected by one check — but it also means an unauthorized user sees the
   shell briefly before redirect. Is that OK, or should we guard at the route
   level?

## Dashboard

3. **Composition bar derivation: client or server?** v1 can derive from
   `getSlips` + `getSlipAssignments`. A dedicated `getMarinaComposition` server
   endpoint is cleaner but costs a backend ticket. Acceptable to defer?

4. **Should the dashboard show the marina name or the date as the H1?** The
   prototype uses the marina name with a season hint as subtitle. The "Today"
   variant used the date. Stick with name-as-title?

## Billing

5. **`getBillingSummary` server endpoint** — needed for KPI tiles without
   pulling every invoice. Worth a follow-up backend ticket, but the screen ships
   without it (we just download invoices and aggregate client-side).

6. **Voided invoices on the dashboard "Open invoices" count?** They have $0
   outstanding so excluding them is correct, but confirm.

## Maintenance

7. **Default `done` window** is 7 days in the prototype. Confirm — or 30 days
   for slower marinas?

8. **Should the New column also have a `since` filter** (e.g. "last 30 days
   only")? Customer-submitted requests can pile up too if nobody triages.

9. **Drag-and-drop later, button-driven now** — confirm OK to defer DnD.

## Listings

10. **Range-drag on the calendar.** The prototype shows the UI; the click-and-
    drag interaction is real and non-trivial to build. Acceptable to ship v1
    with click-on-cell to create a single-day window, then add drag in a
    follow-up?

## Customers

11. **What's the right column header — "Customers" or "Accounts"?** The entity
    is `BillingAccount`. The plain-English label "Customers" is more
    approachable but is a small lie (a single customer can have multiple billing
    accounts). The prototype uses "Customers" — keep?

## Pricing

12. **The existing `PricingPlansPage` has its own layout** (NavBar + max-w-3xl
    body). Folding it into the workspace shell will visibly change its
    presentation. OK to restyle as part of this work, or keep it standalone and
    just add a sidebar entry that links out?

## Settings

13. **Photos — are they live in the existing schema yet?** The codebase has
    `MarinaPhotoDto` and `PhotoCard` but the dashboard photo grid is hidden in
    the wizard. Confirm we can render the same grid in Settings and the data
    flow is shared.

14. **Subscription tab — is plan switching in scope, or just a readout?** The
    prototype shows a "Change plan" button but the existing PricingPlansPage
    is **marina pricing**, not **platform subscription**. A platform-side
    subscription change UI is a separate piece of work. Read-only readout for
    v1?

## Mobile / responsive

15. **Bottom tab bar "More" item** — does it open a Radix `<Sheet>` listing the
    other 8 destinations, or a full-screen menu? Both work; Sheet is more
    iOS-y, full-screen is more Android-y.

## Out-of-band concerns

16. **TypeScript version.** `package.json` pins `typescript: ~6.0.2`. As of this
    writing TS 6 isn't released — this looks like a typo for `5.6.x` or
    similar. Worth confirming and adjusting before Claude Code installs.

17. **Vite 8 + React 19** — both are very recent. If you hit dependency
    weirdness during install, fall back to React 18.3 + Vite 5; nothing in this
    brief depends on a React 19 feature.
