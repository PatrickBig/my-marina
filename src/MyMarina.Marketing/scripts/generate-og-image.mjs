/**
 * Generates public/og-image.png (1200x630) from an SVG template.
 * Run: node scripts/generate-og-image.mjs
 * Called automatically as part of the build via the "prebuild" script.
 */
import sharp from 'sharp';
import { writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const outPath = join(__dirname, '..', 'public', 'og-image.png');

const svg = `
<svg width="1200" height="630" viewBox="0 0 1200 630" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1200" y2="630" gradientUnits="userSpaceOnUse">
      <stop offset="0%" stop-color="#0a1628"/>
      <stop offset="100%" stop-color="#0d2a4a"/>
    </linearGradient>
  </defs>

  <!-- Background -->
  <rect width="1200" height="630" fill="url(#bg)"/>

  <!-- Decorative wave at bottom -->
  <path d="M0 520 Q300 460 600 500 Q900 540 1200 480 L1200 630 L0 630 Z" fill="#0066cc" opacity="0.15"/>
  <path d="M0 560 Q300 500 600 540 Q900 580 1200 520 L1200 630 L0 630 Z" fill="#0066cc" opacity="0.10"/>

  <!-- Anchor icon -->
  <text x="100" y="240" font-family="serif" font-size="96" fill="#0066cc">⚓</text>

  <!-- Logo text -->
  <text x="220" y="220" font-family="Arial, sans-serif" font-size="72" font-weight="700" fill="#ffffff">MyMarina</text>

  <!-- Tagline -->
  <text x="220" y="290" font-family="Arial, sans-serif" font-size="32" fill="#94b8d4">Marina management, simplified.</text>

  <!-- Feature pills -->
  <rect x="220" y="340" width="200" height="44" rx="22" fill="#0066cc" opacity="0.6"/>
  <text x="320" y="368" font-family="Arial, sans-serif" font-size="18" fill="#ffffff" text-anchor="middle">Slip Management</text>

  <rect x="436" y="340" width="180" height="44" rx="22" fill="#0066cc" opacity="0.6"/>
  <text x="526" y="368" font-family="Arial, sans-serif" font-size="18" fill="#ffffff" text-anchor="middle">Bookings</text>

  <rect x="630" y="340" width="180" height="44" rx="22" fill="#0066cc" opacity="0.6"/>
  <text x="720" y="368" font-family="Arial, sans-serif" font-size="18" fill="#ffffff" text-anchor="middle">Invoicing</text>

  <rect x="824" y="340" width="200" height="44" rx="22" fill="#0066cc" opacity="0.6"/>
  <text x="924" y="368" font-family="Arial, sans-serif" font-size="18" fill="#ffffff" text-anchor="middle">Customer Portal</text>

  <!-- Bottom URL -->
  <text x="100" y="580" font-family="Arial, sans-serif" font-size="24" fill="#94b8d4">mymarina.org</text>
</svg>
`.trim();

mkdirSync(join(__dirname, '..', 'public'), { recursive: true });

await sharp(Buffer.from(svg))
  .png()
  .toFile(outPath);

console.log(`Generated ${outPath}`);
