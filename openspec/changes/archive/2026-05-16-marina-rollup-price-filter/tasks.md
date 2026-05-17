## 1. Backend — DTOs and query types

- [x] 1.1 Remove `MinPricePerNight`, `MaxPricePerNight`, and `RateKind` from `MarinaRollupResultDto` in `src/MyMarina.Application/Search/SlipSearchDtos.cs`.
- [x] 1.2 Add `PriceMin` and `PriceMax` (both `decimal?`) to `MarinaRollupSearchQuery` in `src/MyMarina.Application/Search/SlipSearchQueries.cs`.
- [x] 1.3 Add `priceMin` and `priceMax` optional query params to `MarinaSearchController.SearchMarinas`. Return `ValidationProblem` if either is supplied without `listingKind`.

## 2. Backend — rewrite rollup query handler

- [x] 2.1 Rewrite `MarinaRollupSearchQueryHandler` to issue a single EF Core `GroupBy` query: filter slips by bounding-box marina membership, vessel fit, availability (`EXISTS`), and price range (`EXISTS`); group by `MarinaId`; project `{ MarinaId, Count = g.Count(), InstantBook = g.Any(...) }`. No in-memory aggregation.
- [x] 2.2 For the availability `EXISTS` check in the new query: transient path — `AvailabilityWindows` with date range OR direct rate with no conflicts; lease path — `AvailabilityWindows` with lease term OR direct rate with no assignment. Mirror the filter logic already in `SlipAvailabilityFilter` as correlated subqueries.
- [x] 2.3 For the price `EXISTS` check: when `PriceMin`/`PriceMax` are set and `listingKind` is specified, add an additional `WHERE` predicate: slip passes if any matching window price OR its default rate falls within range.
- [x] 2.4 Keep the second query that loads marina details (name, city, state, lat/lon) as a projection from `Marinas` — unchanged from current approach.
- [x] 2.5 Remove `GetRollupSummariesAsync`, `GetTransientRollupSummariesAsync`, `GetLeaseRollupSummariesAsync`, and `RollupSummary` from `SlipAvailabilityFilter.cs`. Confirm `GetEligibleSlipsAsync` (used by `SearchSlipsAtMarinaQueryHandler`) is unaffected.

## 3. Backend — tests

- [x] 3.1 Update existing rollup integration tests: remove assertions on `MinPricePerNight`, `MaxPricePerNight`, `RateKind`; add assertions that those fields are absent from the response shape.
- [x] 3.2 Integration test: `priceMin`/`priceMax` filter with `listingKind=Transient` — marina included when at least one slip has a window price in range, excluded when none do.
- [x] 3.3 Integration test: `priceMin`/`priceMax` filter with `listingKind=Lease` — same inclusion/exclusion logic.
- [x] 3.4 Integration test: supplying `priceMin` without `listingKind` returns `400 Bad Request`.
- [x] 3.5 Run `dotnet test` — all green.

## 4. Frontend — API types

- [x] 4.1 Add `priceMin?: number` and `priceMax?: number` to `MarinaRollupSearchParams` in `src/MyMarina.Web/src/api/api.ts`.
- [x] 4.2 Remove `minPricePerNight`, `maxPricePerNight`, `rateKind` from `MarinaRollupResultDto` in `api.ts`.
- [x] 4.3 Start API server and run `npm run generate-api` from `src/MyMarina.Web/` to regenerate `schema.d.ts`. Do not hand-edit.

## 5. Frontend — search form UI

- [x] 5.1 Add price range inputs (`Price min` / `Price max`) to the search form in `SearchPage.tsx`. Render them only when `listingKind` is selected; hide/clear them when listing kind is cleared.
- [x] 5.2 Pass `priceMin` and `priceMax` through to `searchMarinaRollup` in `runSearchWithBounds`.
- [x] 5.3 Remove any price-related display from the marina rollup result rows (remove `MinPricePerNight`, `MaxPricePerNight`, `RateKind` rendering). Keep `InstantBookAvailable` badge.

## 6. Frontend — build verification

- [x] 6.1 Run `npm run build` from `src/MyMarina.Web/` — clean build, no TypeScript errors.

## 7. Final validation

- [ ] 7.1 Manual smoke test: search with listing kind + price filter → confirm only in-range marinas appear. Search without price filter → all marinas appear. Search with price filter but no listing kind → confirm `400` is handled gracefully in the UI.
- [x] 7.2 Run `openspec validate marina-rollup-price-filter --strict` — no findings.
