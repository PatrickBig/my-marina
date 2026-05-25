## Context

The marketing site (`MyMarina.Marketing`) uses Astro + React + Tailwind. The `ScreenshotsSection` currently lives as an `.astro` component rendering static HTML with a grid of `<img>` elements loaded from `/screenshots/`. There is zero interactivity on these images.

The site already has React 19 + `@astrojs/react` as dependencies, and other marketing components (HeroSection, PricingSection, ContactSection) are already `.tsx` React components. No shadcn/ui exists in the marketing site.

## Goals / Non-Goals

**Goals:**
- Convert `ScreenshotsSection.astro` to a `.tsx` React component
- Add inline `ImageDialog` component for full-screen modal preview
- Add prev/next navigation arrows inside the modal
- Display caption below the image in the modal
- No new npm dependencies

**Non-Goals:**
- Image zoom/pan (literal magnification)
- Keyboard shortcut beyond Escape and left/right arrows
- Swipe gestures on mobile
- Lazy-loading or prefetching full-res images
- Changing the grid layout

## Decisions

### Decision 1: Convert .astro to .tsx
**Choice:** Convert `ScreenshotsSection.astro` to `ScreenshotsSection.tsx`.
**Rationale:** Requires React state (`useState` for selected index), event handlers, and conditional rendering — all need client-side JS. Astro's static rendering can't do this alone.
**Alternatives considered:**
- Astro with an isolated vanilla JS script import — works but clunky inside Astro template, harder to test/reason about.
- Keep as `.astro` with a separate `.ts` handler — splits component logic, unnecessary complexity for 2 interacting components.

### Decision 2: Inline ImageDialog in the same .tsx file
**Choice:** Define `ImageDialog` as a nested function component within `ScreenshotsSection.tsx`.
**Rationale:** The dialog is only used once. Keeping it inline avoids creating a new file for a ~60-line component. Simple to reason about together with the parent.
**Alternatives considered:**
- Separate `ImageDialog.tsx` file — overkill for single-use, adds friction for future edits.
- shadcn Dialog component — no shadcn in marketing site, adding that dependency is heavy.

### Decision 3: Pure Tailwind for modal styling
**Choice:** Use Tailwind utility classes for all modal styling.
**Rationale:** Tailwind is already the styling system. No need for CSS modules or global styles.
```
Tailwind classes:
  Backdrop:  fixed inset-0 bg-black/80 z-50 flex items-center justify-center
  Card:       relative bg-black/90 rounded-xl max-w-4xl w-full mx-4
  Image:      w-full object-contain max-h-[85vh]
  Caption:    text-center text-white/90 text-sm font-medium px-6 py-4
  Nav arrows: absolute left-4/4/2 top-1/2 -translate-y-1/2
  Close:      absolute right-4 top-4 m-2
```

### Decision 4: Wrapping navigation
**Choice:** Left on first item goes to last; right on last item goes to first.
**Rationale:** Common pattern (Carousels, Figma, Lightbox libraries). Better UX than disabling buttons.

### Decision 5: Use index-based state instead of src-based state
**Choice:** `useState<number | null>(null)` for selected index.
**Rationale:** Simplifies prev/next navigation logic — just `index - 1` / `index + 1`. If we used src strings, navigation would require `.findIndex` on every step.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| React hydration mismatch if Astro SSR + client components diverge | Component uses `useState` initialized with `null` — same on server and client. No prop-driven initial state. |
| Large screenshot PNGs slow to load in modal | Images are already loaded by the browser (cached on thumbnail load). Modal just swaps `src` of existing array items. |
| Accessibility: screen reader users can't use image gallery | Add `role="dialog"`, `aria-label`, `aria-modal="true"`, and keyboard focus trap in tasks. |
| Mobile touch: prev/next arrows may be too close to edges on small screens | Use `p-2` padding on arrow buttons; arrows shift to `left-2`/`right-2` on mobile via responsive classes. |

## Open Questions

None identified. All decisions made in discovery.
