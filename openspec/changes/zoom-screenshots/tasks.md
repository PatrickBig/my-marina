## 1. Convert Astro Component to React

- [x] 1.1 Rename ScreenshotsSection.astro to ScreenshotsSection.tsx
- [x] 1.2 Convert Astro data section to a `const screenshots` array in TypeScript
- [x] 1.3 Convert Astro template JSX to React JSX (use className instead of class)
- [x] 1.4 Add `useState` import for selection state management

## 2. Add ImageDialog Component

- [x] 2.1 Define `ImageDialog` React component at the top of ScreenshotsSection.tsx
- [x] 2.2 Implement modal backdrop (fixed inset-0 bg-black/80 z-50)
- [x] 2.3 Implement centered card container (bg-black/90 rounded-xl max-w-4xl)
- [x] 2.4 Add image display with object-contain and max-h-[85vh]
- [x] 2.5 Add caption section below image (text-center text-white/90)
- [x] 2.6 Add close (×) button in top-right corner
- [x] 2.7 Implement backdrop click → close
- [x] 2.8 Implement Escape key → close
- [x] 2.9 Implement close button → close

## 3. Add Prev/Next Navigation

- [x] 3.1 Add left arrow button (absolute left side, top-1/2 -translate-y-1/2)
- [x] 3.2 Add right arrow button (absolute right side, top-1/2 -translate-y-1/2)
- [x] 3.3 Implement prev navigation (currentIndex - 1, wrap to end)
- [x] 3.4 Implement next navigation (currentIndex + 1, wrap to start)
- [x] 3.5 Hide left arrow when first item, hide right arrow when last item
- [x] 3.6 Implement keyboard left/right arrow navigation in modal

## 4. Wire Up Click-to-Open and State Management

- [x] 4.1 Add `selectedSrc` and `currentIndex` state variables
- [x] 4.2 Add onClick handler on each thumbnail figure to open modal
- [x] 4.3 Add visual highlight (ring or border) on the clicked thumbnail
- [x] 4.4 Update caption and image source when navigating in modal
- [x] 4.5 Prevent body scroll when modal is open

## 5. Polish

- [x] 5.1 Add hover cursor and subtle transition on thumbnail click
- [x] 5.2 Ensure responsive sizing for mobile (arrow positions, max width)
- [x] 5.3 Add role="dialog" and aria-label to modal for accessibility
- [x] 5.4 Verify image gallery grid is visually unchanged before modal interaction
