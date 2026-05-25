## Why

The marketing site's `ScreenshotsSection` displays 6 product screenshots as small thumbnails in a grid. Visitors cannot see the UI details these screenshots are meant to showcase — they are too small to read or appreciate. This reduces the persuasive value of a key marketing asset.

## What Changes

- Convert `ScreenshotsSection.astro` to `ScreenshotsSection.tsx` (React component) to enable click-to-open modal state
- Inline an `ImageDialog` component within the file — a full-screen backdrop overlay with centered screenshot image, caption below the image, and left/right navigation arrows
- Close modal via backdrop click, Close (×) button, or pressing Escape
- No new external dependencies

## Capabilities

### New Capabilities
- `image-preview`: Modal image viewer with caption, prev/next navigation for marketing site screenshots

### Modified Capabilities
<!-- None — this is a new capability, not modifying existing spec-level requirements -->

## Impact

- `src/MyMarina.Marketing/src/components/ScreenshotsSection.astro` → converted to `.tsx`
- No new npm dependencies
- No API changes
- Marketing site build pipeline unchanged (Astro + React already configured)
