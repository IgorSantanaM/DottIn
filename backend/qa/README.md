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
