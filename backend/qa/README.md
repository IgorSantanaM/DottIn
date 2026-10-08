# MVP release gate

Run the automated gate from the repository root:

```powershell
& ./backend/qa/verify-mvp.ps1
```

It runs all four test projects, builds the web Admin in Release, and builds Android in Debug. On a machine without Android tooling, use `-SkipAndroid` and run the Android build on a supported build agent.

With the Admin and API deployed to a test environment backed by PostgreSQL, also check their HTTP surfaces:

```powershell
& ./backend/qa/verify-mvp.ps1 -AdminBaseUrl https://admin.example.test -ApiBaseUrl https://api.example.test
```

The API must return `200` from both `/health/live` and `/health/ready`; readiness returns `503` if the database cannot be reached within three seconds. The API must send `Server-Timing` on the liveness response. The script does not create test accounts, alter data, or make Stripe payments.

Before release, complete the device and external-service checklist in [MOBILE_PARITY.md](../clients/DottIn.Mobile/MOBILE_PARITY.md). In particular, verify session restoration, GPS permission and denied-permission flows, responsive web layout, real CSV/TXT downloads, cross-tenant kiosk restrictions, Stripe test-mode checkout/webhooks/portal, and Domínio import against a test company. Supply official mobile Terms, Privacy, and Support destinations at build time. Resolve the MediatR production-license warning shown at API startup, and configure the production secrets and persistent Data Protection keys listed in the root README.
## Admin browser E2E

Apply `../tools/seed_demo_august_2026.sql`, start the API on `http://localhost:5101` and Admin on `http://localhost:5231`, then run:

```powershell
Set-Location backend/qa
npm install --ignore-scripts
npm run test:admin:e2e
```

The suite uses the locally installed Chrome and covers owner, administrator, and employee login, CPF masking, dashboard rendering, header/content layout, August history, role-specific history authorization, the main management pages, and session restoration after refresh. It also validates a geolocated web punch flow, authorized break-window rejection, Web audit source, Domínio employee mappings, CSV and Domínio TXT downloads and contents, billing reads, and unsigned Stripe webhook rejection. It also checks cross-tenant API reads in both directions for branches, employee directories, invitations, adjustments, calendars, timekeeping history, and exports (all must return 403), plus refresh-token rotation and immediate logout revocation of both refresh and access tokens. It never creates a real Stripe charge or changes a subscription.
## Credential revocation regression

After building the API in Debug, run `./backend/qa/verify-credential-revocation.ps1` from the repository root. It creates a new disposable PostgreSQL database, applies migrations and the synthetic August seed, starts an isolated API on `127.0.0.1:5102`, then checks weak-password rejection, password/PIN changes, immediate access/refresh-token revocation, and login with the new credentials. It removes only its own temporary database and container seed file. It never changes `dottindb` or makes a Stripe call.

## Company join-link regression

After building the API in Debug, run `./backend/qa/verify-company-join-link.ps1` from the repository root. It uses the same disposable PostgreSQL setup on `localhost:5102` to verify owner-only link issuance, anonymous resolution, new-account registration and immediate branch access, existing-account association, rejection of duplicate CPF and accounts assigned to another branch, and revocation of an old unassigned-owner session. It removes only its temporary database and seed file. Add `-WithBrowser` when the Admin is running at `http://localhost:5231`, or pass `-AdminUrl http://localhost:5233` for another local test port. The optional Chrome test copies the link from `/employees`, registers through `/join`, checks the employee role in the header and menu, enters the dashboard and verifies session restoration after refresh. No production data, deployment or Stripe payment is involved.
## Stripe sandbox onboarding

Use the same Stripe **test account** for the API secret key and the three active monthly BRL price IDs. Seed local plans with `../tools/seed_stripe_test_plans.sql` using psql variables `basic_price`, `professional_price`, and `unlimited_price`. Never apply this seed to production.

With the API and Admin running locally, opt in to a new registration and Checkout test:

```powershell
Set-Location backend/qa
$env:DOTTIN_STRIPE_E2E = '1'
npx playwright test -c playwright.config.cjs stripe-registration-e2e.spec.cjs
```

The test creates a synthetic owner, first branch, Stripe test customer, and subscription-mode Checkout Session, then verifies the local Free state before payment. To resume an owner from a failed attempt, also set `DOTTIN_STRIPE_QA_RUN_ID` to its original run ID. To complete a test payment, set `DOTTIN_STRIPE_QA_CHECKOUT_URL` to that test session's URL and run `stripe-checkout-payment-e2e.spec.cjs`; it uses Stripe's test card and never charges real money. Finally, forward Stripe's signed `checkout.session.completed` event to `/api/webhooks/stripe` using a listener whose signing secret matches `Stripe:WebhookSecret`, then verify the local plan is Active. These tests are opt-in because they mutate the local database and the Stripe sandbox.
For automatic webhook delivery in local test mode, start a Stripe CLI listener before payment:

```powershell
stripe listen --forward-to http://localhost:5101/api/webhooks/stripe --events checkout.session.completed,customer.subscription.updated,customer.subscription.deleted,invoice.payment_succeeded,invoice.payment_failed
```

Confirm that the listener's signing secret matches the API's `Stripe:WebhookSecret`. The paid-owner browser test is opt-in with `DOTTIN_STRIPE_QA_CPF` and `DOTTIN_STRIPE_QA_PASSWORD`; `DOTTIN_STRIPE_QA_SESSION_ID` additionally checks the post-Checkout success page. Run `stripe-paid-admin-e2e.spec.cjs` to verify the active plan and Billing Portal. In the sandbox, canceling from the portal schedules access until period end (`customer.subscription.updated`); an actual deletion (`customer.subscription.deleted`) reverts the local plan to Free. A new Checkout must work immediately after an expired or canceled Checkout, and again after returning to Free.

## Local PostgreSQL migration and recovery smoke tests

With the development PostgreSQL Docker container running and a Debug build of `DottIn.Infra.Data`, run from the repository root:

```powershell
& ./backend/qa/verify-postgres-migrations.ps1
& ./backend/qa/verify-postgres-recovery.ps1 -VerifyUpgrade
```

The first script creates a uniquely named empty database, applies all EF migrations twice, and requires exactly one active Free plan. The second dumps `dottindb`, restores it into a separate temporary database, compares key row counts, applies pending migrations only to that copy, and checks that the existing Free plan ID and tenant data remain intact. Both scripts remove their exact temporary database (and the restore dump) on completion; neither deploys code or changes another environment. Run them only against the local development Docker/PostgreSQL setup. Paid Stripe plans still require environment-specific active Price IDs and are not provisioned by the Free-plan migration.
## Local API latency baseline

Start the API locally with the synthetic August seed, then set `DOTTIN_QA_CPF` and `DOTTIN_QA_PASSWORD` for an owner account and run:

```powershell
node backend/qa/measure-local-api.cjs
```

The script refuses non-localhost URLs. It samples five logins and fifteen sequential reads each for dashboard, paged history, and CSV, consuming response bodies before measuring p50/p95. Optional `DOTTIN_QA_LOGIN_SAMPLES` (1–10) and `DOTTIN_QA_READ_SAMPLES` (1–50) adjust counts. It prints no credential or token. Run once cold and again warm to distinguish query compilation/first-use effects. This is a development baseline with concurrency 1, not an acceptance load test; performance targets and realistic concurrency require the pilot scope and homologation environment.
## Session security

Access tokens default to 15 minutes. Production configuration rejects `JwtSettings:ExpirationMinutes` outside 1–30. Web and mobile refresh proactively; mobile serializes concurrent refreshes and retries only safe reads after 401. Logout revokes stored refresh tokens and clears local credentials, but an already issued stateless JWT remains valid until its expiry. Do not describe logout as immediate server-side access-token invalidation; that would require a separate revocation design and acceptance test.
