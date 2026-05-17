## Context

The `marina-rollup-slip-search` change shipped a `GET /marinas/search` endpoint that aggregates available slip counts per marina. The current implementation (`MarinaRollupSearchQueryHandler`) calls `GetRollupSummariesAsync` in `SlipAvailabilityFilter`, which projects `(MarinaId, Price, RateKind, InstantBook)` tuples into .NET memory and then groups them there. This was necessary to compute `MinPricePerNight`, `MaxPricePerNight`, and `RateKind = "Mixed"` for display. It violates the CLAUDE.md "built to scale" mandate, which requires DB-level aggregation.

Removing price display from the rollup removes the need for per-slip price materialization. A price-range *filter* replaces it — implemented as `EXISTS` subqueries which SQL handles natively without aggregation. The result is a single `GROUP BY` query at the DB layer.

## Goals / Non-Goals

**Goals:**
- Eliminate in-memory grouping from the marina rollup query — single DB-level `GROUP BY COUNT`.
- Add `priceMin`/`priceMax` filter parameters to `GET /marinas/search`, applied only when `listingKind` is set.
- Price filter semantics: a slip qualifies if ANY matching window (or its default rate) falls within the range — an `EXISTS` check, not a `MIN/MAX` aggregation.
- Remove `GetRollupSummariesAsync` / `RollupSummary` from `SlipAvailabilityFilter`.
- Update the search form UI to expose price range inputs when listing kind is selected.

**Non-Goals:**
- Price display in the rollup list — deferred entirely (may revisit post-scale with a materialized view).
- Per-slip price filtering in `GET /marinas/{id}/slips/search` — `SlipSearchResultDto` already includes prices; a price filter there is a separate change if needed.
- Currency conversion or locale-specific formatting.

## Decisions

### 1. True DB-level `GROUP BY` via EF Core LINQ

The rewritten handler issues a single EF Core query:

```csharp
await db.Slips
    .Where(s => s.Status == Active
             && marinaIds.Contains(s.MarinaId)
             && [vessel fit]
             && [availability EXISTS]
             && [price EXISTS, if filter provided])
    .GroupBy(s => s.MarinaId)
    .Select(g => new { MarinaId = g.Key, Count = g.Count(),
                       InstantBook = g.Any(s => [instant book EXISTS]) })
    .ToListAsync(ct);
```

EF Core translates `GroupBy + Count + Any` to a single SQL `SELECT marina_id, COUNT(*), BOOL_OR(...)  FROM slips WHERE ... GROUP BY marina_id`. No .NET memory aggregation.

**Alternative considered — raw SQL:** More control, but harder to maintain and bypasses EF Core's query filter safety. Rejected in favor of EF LINQ given the query is expressible without raw SQL.

### 2. Price filter as `EXISTS`, not `JOIN`

A price range filter asks "does this slip have ANY matching window priced in range?" — an `EXISTS` correlated subquery, not a `JOIN`. This is semantically correct and more efficient: the DB stops at the first matching row rather than enumerating all windows.

For direct-rate slips (Path B), the price check is a simple column predicate (`s.DefaultTransientBaseRate BETWEEN @min AND @max`), no subquery needed.

**Alternative considered — `JOIN` + `HAVING`:** Would require a join to AvailabilityWindows before the GroupBy, inflating the row set. Rejected.

### 3. Price filter scoped strictly to `listingKind`

`priceMin`/`priceMax` are ignored by the handler when `listingKind` is null. Transient price is per-night; Lease price is per-period. Mixing units across listing kinds is meaningless. The UI enforces this by only rendering price inputs when a listing kind is selected.

**Alternative considered — separate `transientPriceMax` / `leasePriceMax` params:** Cleaner API contract but more complexity for MVP. Rejected — `listingKind` gating is sufficient.

### 4. Remove `GetRollupSummariesAsync` entirely

The method was introduced specifically to feed price aggregation. With prices removed from the rollup, it has no other caller. Keeping it would be dead code. `GetEligibleSlipsAsync` (used by `SearchSlipsAtMarinaQueryHandler`) is unaffected and stays.

### 5. Marina details still fetched separately

After the `GROUP BY` query, we still need marina names, city/state, and lat/lon for the DTO. A second query projecting only those fields from `Marinas` is required. This is acceptable — it's bounded by the number of marina rows in the result (small), not the number of slips.

**Alternative considered — single query with JOIN to Marinas:** EF Core GroupBy with a Join can be tricky to translate. The two-query approach is clearer and the marina lookup is O(results), not O(slips).

### 6. `InstantBookAvailable` stays in the rollup

It's a simple `EXISTS` check at the slip level — no price data required. It's a meaningful differentiator that costs nothing extra in the new query.

## Risks / Trade-offs

- **`EXISTS` subqueries add correlated query cost** → Mitigated by existing indexes on `SlipId` in `AvailabilityWindows` and `SlipAssignments`. The bounding-box + vessel-fit pre-filter keeps the slip set small before the EXISTS runs.
- **EF Core GroupBy translation can be fragile** → If EF Core cannot translate a specific `Any()` expression inside a `GroupBy`, it will throw at runtime (not compile time). Cover with integration tests that exercise both the price-filtered and unfiltered paths.
- **Price filter ignores listing kind when unset** → A caller passing `priceMin` without `listingKind` gets it silently ignored. Document in OpenAPI description; add a validation warning in the controller if `priceMin`/`priceMax` are set without `listingKind`.

## Migration Plan

1. Backend changes compile and pass tests — no DB schema change required (no new columns or indexes).
2. Frontend `schema.d.ts` regenerated after API server reflects updated `MarinaRollupResultDto`.
3. No data migration — removing fields from the response is non-breaking for consumers (frontend is the only consumer and is updated in the same PR).
4. Rollback: revert the handler and DTO changes. The `GetRollupSummariesAsync` method is deleted but can be restored from git history.
