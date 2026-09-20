# DottIn

## Stripe local setup

The webhook endpoint accepts both `/api/webhooks/stripe` and `/webhook`. The API development profile listens on port `4242`, so Stripe CLI can be started with:

```powershell
stripe listen --forward-to http://localhost:4242/webhook
```

Store Stripe credentials in the Web API user-secrets store; never add them to `appsettings*.json`:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project backend/src/DottIn.Presentation.WebApi
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project backend/src/DottIn.Presentation.WebApi
```

Also configure valid Stripe price IDs in the `SubscriptionPlans.StripePriceId` database column. Checkout is unavailable for plans without a price ID. In production provide `Stripe:SecretKey`, `Stripe:PublishableKey`, `Stripe:WebhookSecret`, `Stripe:SuccessUrl`, `Stripe:CancelUrl`, and `Stripe:PortalReturnUrl` through deployment secrets/environment variables.
