## Why

The marina rollup search currently aggregates min/max prices per marina, which forces an in-memory grouping step that cannot be pushed to the database — a direct violation of the "built to scale" mandate in CLAUDE.md. Removing price display from rollup results and replacing it with a price-range filter resolves the performance constraint, simplifies the aggregation to a true DB-level `GROUP BY COUNT`, and gives boaters a cleaner way to express their budget upfront rather than scanning a confusing mix of PerFoot and Flat rate figures in a list.

## What Changes

- **Remove** `MinPricePerNight`, `MaxPricePerNight`, and `RateKind` fields from `MarinaRollupResultDto` — marinas no longer display price ranges in the rollup list.
- **Add** `PriceMin` and `PriceMax` optional filter parameters to `GET /marinas/search`, scoped per `listingKind`:
  - `Transient`: filter applies to `AvailabilityWindow.BasePricePerNight` (per-night) and `Slip.DefaultTransientBaseRate`.
  - `Lease`: filter applies to `AvailabilityWindow.BasePricePerNight` (per-period) and `Slip.DefaultLeaseBaseRate`.
  - Price filter is ignored when `listingKind` is not set.
- **Rewrite** `MarinaRollupSearchQueryHandler` to use a single DB-level `GROUP BY` query with `EXISTS` subqueries for availability and price checks — eliminates `GetRollupSummariesAsync` and all in-memory aggregation.
- **Update** the rollup frontend search form to expose price range inputs when a listing kind is selected.
- **Remove** `GetRollupSummariesAsync` and `RollupSummary` from `SlipAvailabilityFilter` (no longer needed).
- **Update** `MarinaRollupResultDto` and `MarinaRollupSearchParams` TypeScript types (regenerate `schema.d.ts`).

## Capabilities

### New Capabilities

None — this change modifies existing slip-search behavior only.

### Modified Capabilities

- `slip-search`: Marina rollup result shape changes (price fields removed); rollup search gains `priceMin`/`priceMax` filter params scoped by `listingKind`.

## Impact

- **Backend**: `MarinaRollupSearchQueryHandler.cs`, `SlipAvailabilityFilter.cs`, `MarinaSearchController.cs`, `SlipSearchDtos.cs`, `SlipSearchQueries.cs`
- **Frontend**: `SearchPage.tsx` (price filter inputs), `api.ts` (`MarinaRollupSearchParams`, `MarinaRollupResultDto`), `schema.d.ts` (regenerated)
- **Tests**: Integration tests for the rollup endpoint need updating — assertions on price fields removed, new assertions for price filter behavior added
- **No breaking change to the per-marina slip search** (`GET /marinas/{id}/slips/search`) — prices remain in `SlipSearchResultDto`
