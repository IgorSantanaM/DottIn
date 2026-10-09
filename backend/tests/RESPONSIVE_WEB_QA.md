# Web/Admin responsive QA

Validated locally on 2026-10-09 against the published Docker Admin and API,
using the August 2026 demo seed in a separate `dottin-responsive-qa` database.
No production deployment or production data changes.

## Implementation

- Desktop mini navigation and mobile temporary navigation share one
  `OperationalNavMenu` component and the existing permission checks.
- CSS selects navigation immediately at 960 px, including live resizing.
  Mobile navigation starts closed and closes on route changes.
- Fixed header reserves 72 px on desktop/tablet and 76 px on phones.
- Employee search and status controls stack on phones; labels and data remain visible.
- Holiday cards wrap metadata and keep their action button within the card.
- Dashboard clock uses fluid typography and tabular numerals; punch controls
  and the day summary adapt without changing timekeeping or geolocation rules.
- Mobile tables, dialogs, pagination and snackbars wrap content. Wide tables
  retain local scrolling instead of hiding document overflow.
- The CSS URL is versioned to invalidate the previous seven-day static cache.

## Verification

- Admin regression suite: 74 passed; Mobile regression suite: 57 passed.
- Release Admin publish/container build successful; no new Admin compiler warnings.
- Employee page and personal dashboard: 320, 375, 390, 414, 768 and 1366 px.
  Document width did not exceed viewport width at any tested size.
- Holidays: the same six widths; expanded calendar and worked-on-holiday data
  inspected at 320 and 390 px.
- Initial page title starts at 76 px below a 56 px phone header, or 72 px below
  a 48 px desktop/tablet header.
- Live desktop-to-phone resize, mobile menu opening, route change closing,
  desktop navigation and authenticated refresh verified.
- Employee status filter and debounced name search verified with demo records.
- Owner history query loaded 25 of 46 demo records with pagination; employee
  history uses the personal view. No timekeeping records were changed.
- New holiday dialog inspected at 320 px and cancelled without saving.
- Subscription view inspected at 320 px; no billing actions submitted.
- Light/dark themes and reactive logo variants verified.
- Personal dashboard preserves the employee role and does not expose owner billing.
- Invalid date range exercised to validate a multiline, dismissible mobile toast.

## Manual follow-up on a physical phone

The automated browser viewport is not an Android or iOS device emulator.
Before a future release, smoke-test Android Chrome and iOS Safari, especially
browser chrome/safe-area changes, the virtual keyboard, portrait/landscape
rotation and a real permission-approved geolocation request. No location
permission was granted and no punch action was submitted during this UI QA.

## Reproduce locally

Use the repository's configured local `.env` and compose overlay. Start an
isolated project (never run the seed against production or an existing database):

```powershell
docker compose -p dottin-responsive-qa up -d --build --wait
docker cp backend/tools/seed_demo_august_2026.sql dottin-responsive-qa-postgres-1:/tmp/responsive-qa-seed.sql
docker exec dottin-responsive-qa-postgres-1 psql -U dottin -d dottindb -v ON_ERROR_STOP=1 -f /tmp/responsive-qa-seed.sql
dotnet test backend/tests/DottIn.Admin.Tests/DottIn.Admin.Tests.csproj
dotnet test backend/tests/DottIn.Mobile.Tests/DottIn.Mobile.Tests.csproj
```

Use the demo owner and employee from the seed at `http://localhost:32850`.
After testing, remove only this disposable stack and its synthetic data:

```powershell
docker compose -p dottin-responsive-qa down --volumes
```
