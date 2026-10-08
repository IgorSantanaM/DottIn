# DottIn

## Local Docker setup (Windows / PowerShell)

From the repository root, run:

```powershell
./backend/tools/Initialize-ComposeEnvironment.ps1
docker compose config --quiet
docker compose up -d --build
```

The initializer creates a private, Git-ignored `.env` beside `compose.yaml`, generates independent PostgreSQL, RabbitMQ, JWT and local storage secrets, and preserves existing passwords on reruns. It imports an existing **test-mode pair** of Stripe keys from the API's .NET user-secrets store, falling back to the authenticated Stripe CLI's default profile. Run `stripe login` first if necessary. It retrieves the local listener signing secret with Stripe CLI without printing credentials. If credentials are unavailable, it reports the missing setting names; it does not insert fake Stripe keys.

Local mode sets `APP_PUBLIC_URL=http://localhost:32850` and selects `compose.yaml` plus `compose.local.yaml` through `.env`. The API uses Development mode so local HTTP works. The overlay adds a persistent Azurite Blob Storage emulator on **127.0.0.1:32855** (container port 10000), avoiding any need for a real Azure subscription. The Blob connection string uses the container-internal endpoint; it is for the backend, not a public photo URL. Without `MT_LICENSE`, the initializer explicitly selects core-only mode: RabbitMQ runs, but background consumers, including employee image processing, remain disabled. Provide the license and set `MASSTRANSIT_DISABLED=false` to test those features.

For Stripe webhooks, keep this running in a separate terminal using the **same Stripe test account** as the configured keys:

```powershell
stripe listen --forward-to http://localhost:32850/api/webhooks/stripe
```

If the signing secret changes, run `./backend/tools/Initialize-ComposeEnvironment.ps1 -RefreshWebhookSecret` and `docker compose up -d` to recreate the API with the updated value. The initializer does not start containers, create live payments or deploy anything. Stripe paid plans still require the correct test Price IDs in the database. Localhost HTTP works on this computer; smartphone geolocation over a LAN IP requires an HTTPS origin.

The `.env` file is **not transferred by Git**. If the checkout is at `E:\Projetos\DottIn`, run the initializer there, or securely copy your `.env` to that checkout. Do not regenerate database passwords for existing volumes; if credentials were lost, recover the old password or explicitly rotate it in PostgreSQL rather than deleting data.

For production, use `./backend/tools/Initialize-ComposeEnvironment.ps1 -Mode Production -PublicUrl https://your-domain.example` with your real Azure, Stripe endpoint and license settings provided through environment variables or `.env`. The initializer refuses to switch an existing localhost configuration to production (or the reverse) implicitly; use a separate checkout/private environment file. Production selects `compose.yaml` only, without the emulator or HTTP development overlay.

## Docker deployment

The root `compose.yaml` follows the portfolio's deployment pattern: build locally, serve the web Admin with Nginx on container port 80, publish fixed host ports, and restart services with `unless-stopped`. It runs the web Admin, API, PostgreSQL and RabbitMQ. The static mockup in `frontend/` and the native mobile app are not container services.

Copy `.env.example` to `.env` at the repository root, set `APP_PUBLIC_URL` to your public HTTPS origin **without a trailing slash**, and fill in the database, RabbitMQ, JWT, Azure Blob and Stripe settings. Generate independent URL-safe passwords, for example with `openssl rand -hex 32`. The Compose file refuses to start with missing required values. `.env` is ignored by Git and excluded from Docker build contexts.

The existing MassTransit 9 packages require a valid license when messaging is enabled. Set `MT_LICENSE` in `.env` for the full stack. For a core-only stack, explicitly set `MASSTRANSIT_DISABLED=true`; RabbitMQ still runs, but the API does not process background messages such as employee image uploads. The default keeps messaging enabled and the API will refuse to start without its license. The existing MediatR package also reports a production license requirement; `MEDIATR_LICENSE_KEY` passes its key to the application.

From the repository root:

```sh
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

| Service | Host port | Container port | Default host binding |
| --- | ---: | ---: | --- |
| Web Admin / Nginx | 32850 | 80 | All interfaces |
| Backend API | 32851 | 8080 | All interfaces |
| PostgreSQL | 32852 | 5432 | 127.0.0.1 |
| RabbitMQ AMQP | 32853 | 5672 | 127.0.0.1 |
| RabbitMQ management UI | 32854 | 15672 | 127.0.0.1 |

Point the host's HTTPS reverse proxy at `http://127.0.0.1:32850`, preserve `Host`, and send `X-Forwarded-Proto: https` and `X-Forwarded-For`. Nginx serves client-side routes such as `/dashboard` and `/join`, and forwards `/api/`, `/health/` and `/webhook` to the API. The browser uses this same origin for API calls, so a separate public API domain is optional; if needed, proxy that domain to port 32851. The outer proxy should redirect public HTTP traffic to HTTPS. TLS is terminated by the host proxy, as with the portfolio.

The API trusts forwarded headers from the Compose network (`172.28.85.0/24` by default). Change `COMPOSE_SUBNET` if that network is already in use. The same value configures Docker IPAM and the API's trusted network. If an outer proxy connects from another network, add its specific network to `ReverseProxy__KnownNetworks__1` in the deployment configuration. `TOOLS_BIND_ADDRESS` can change the bind address of infrastructure ports. RabbitMQ management is available locally at `http://localhost:32854`, with user `dottin` and the configured RabbitMQ password.

Named volumes persist PostgreSQL data, RabbitMQ state and invitation-signing keys. The API runs as the .NET non-root user and its key directory is writable by that user. On initial startup it applies EF migrations and seeds the Free plan; `APPLY_MIGRATIONS_ON_STARTUP=false` disables this when migrations are managed separately. Existing Stripe paid plans still need valid Price IDs in the database. Azure Blob Storage and Stripe remain external services and do not publish local ports. No demo data is loaded automatically.

`docker compose down` stops the stack and retains its volumes. Run Docker Compose 2.20 or newer; `backend/docker-compose.yml` includes the root configuration for compatibility with commands run from `backend/`.

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

## Production key persistence

Company invitation links use ASP.NET Core Data Protection. Set `DataProtection__KeysDirectory` to an absolute path on a persistent, private volume before starting the production API. Share the same key directory across API replicas and keep it through deployments and backups; losing the keys invalidates outstanding invitation links. Restrict filesystem access to the API process and encrypt the volume at rest. The production configuration validator refuses startup without this setting. Development keeps the default local key storage.
