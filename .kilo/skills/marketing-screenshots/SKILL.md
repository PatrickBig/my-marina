---
name: marketing-screenshots
description: Capture screenshots of the MyMarina marketing site and web app for the marketing landing page using Docker Compose for the dev environment.
allowed-tools: Bash(docker-compose:*) Bash(playwright-cli:*) Bash(npx:*)
---

# Marketing Screenshots Capture

Capture screenshots for the marketing site using the full Docker Compose dev environment (Postgres, Redis, API, web, marketing).

## Prerequisites

The Docker Compose stack must be running. The marketing site is served at `http://localhost:4321`.

```bash
# Start the full stack
docker-compose up -d

# Wait for services to be healthy
docker-compose ps
```

Verify the marketing site is accessible:
```bash
playwright-cli open http://localhost:4321
playwright-cli snapshot
```

## Capturing screenshots — default flow

### 1. Marketing landing page

```bash
playwright-cli open http://localhost:4321
playwright-cli resize 1920 1080
playwright-cli screenshot --filename=index.png
```

### 2. Authenticated web app pages

The web app is at `http://localhost:5173`. Use the login credentials configured in `docker-compose.yml`:

| Role | Email | Password |
|------|-------|----------|
| Platform operator | `platform@mymarina.org` | `Testpassword1!` |
| Marina operator | `marina@mymarina.org` | `Testpassword1!` |

Log in as a platform operator to access the operator dashboard:

```bash
playwright-cli open http://localhost:5173/login
playwright-cli snapshot

playwright-cli fill e1 "platform@mymarina.org"
playwright-cli fill e2 "Testpassword1!"
playwright-cli click e3  # Log In button
playwright-cli reload   # wait for redirect

playwright-cli resize 1920 1080
playwright-cli screenshot --filename=operator-dashboard.png
```

Log in as a marina operator for the marina dashboard:

```bash
playwright-cli open http://localhost:5173/login
playwright-cli fill e1 "marina@mymarina.org"
playwright-cli fill e2 "Testpassword1!"
playwright-cli click e3
playwright-cli screenshot --filename=marina-dashboard.png
```

Navigate to specific pages after login:

```bash
playwright-cli goto http://localhost:5173/invoices
playwright-cli screenshot --filename=invoicing.png

playwright-cli goto http://localhost:5173/slips
playwright-cli screenshot --filename=slips-list.png

playwright-cli goto http://localhost:5173/profile
playwright-cli screenshot --filename=profile.png
```

### 3. Save the file to `src/MyMarina.Marketing/public/screenshots/`

```bash
# Copy from the .playwright-cli temp directory (where playwright-cli saves screenshots)
cp .playwright-cli/index.png ../src/MyMarina.Marketing/public/screenshots/operator-dashboard.png
cp .playwright-cli/invoicing.png ../src/MyMarina.Marketing/public/screenshots/invoicing.png
```

Update `src/MyMarina.Marketing/src/components/ScreenshotsSection.astro` if adding new screenshots.

### 4. Verify the page renders

```bash
playwright-cli close
```

## Login form element refs

The login form at `http://localhost:5173/login` typically maps to:
- `e1` — email input
- `e2` — password input
- `e3` — Log In button

Always run `playwright-cli snapshot` first to confirm refs for the current page version.

## Logout pattern (when needed)

```bash
playwright-cli click "button:has-text(\"Log Out\"), button[aria-label=\"User menu\"]"
playwright-cli close
```

## Common page URLs

| Page | URL |
|------|-----|
| Marketing home | `http://localhost:4321` |
| Web app home | `http://localhost:5173/` |
| Login | `http://localhost:5173/login` |
| Operator dashboard | `http://localhost:5173/` (after login) |
| Marina dashboard | `http://localhost:5173/dashboards/[marina-slug]` |
| My slips | `http://localhost:5173/slips` |
| My invoices | `http://localhost:5173/invoices` |
| My boats | `http://localhost:5173/boats` |
| My profile | `http://localhost:5173/profile` |
| Marina slips | `http://localhost:5173/marina/[slug]/slips` |
| Search | `http://localhost:5173/search` |
| Marina setup wizard | `http://localhost:5173/marina/setup` |
| Marina onboarding | `http://localhost:5173/marina/onboard` |
| Private dock onboarding | `http://localhost:5173/private-dock/onboard` |
| Customer portal | `http://localhost:5173/portal` |
| Maintenance | `http://localhost:5173/maintenance` |

## Troubleshooting

### Docker Compose not starting

```bash
docker-compose build marketing
docker-compose up -d
```

### API not ready — CORS errors

Wait for the `api-setup` service to complete:
```bash
docker-compose logs -f api
```

### CORS issues for localhost:4321

The API env in `docker-compose.yml` includes `http://localhost:4321` in `Cors__AllowedOrigins__1`. If modifying the setup, ensure this stays in sync.

### Screenshot not capturing fully visible content

```bash
playwright-cli resize 1920 1080
playwright-cli screenshot --filename=page.png
```

Or adjust the viewport to match the desired output size.

### Stale browser session

```bash
playwright-cli close
playwright-cli open http://localhost:4321
```
