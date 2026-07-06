# Local Docker Environment

`docker-compose.yml` runs the local BetterBooking stack:

- PostgreSQL.
- Local-only OIDC provider for development sign-in.
- EF Core database migrations.
- Local demo seed data.
- ASP.NET Core API.
- Next.js frontend.

Start it from the repository root:

```bash
docker compose --file infra/local/docker-compose.yml up --build
```

Then open `http://localhost:3000`.

## Generated Local Secrets

The `local-secret-init` service generates local-only PostgreSQL and frontend cookie secrets into a Docker named volume. These values are not stored in source files.

Delete and regenerate local secrets and database data only when you intentionally want a clean environment:

```bash
docker compose --file infra/local/docker-compose.yml down --volumes
```

## Ports

- Frontend: `127.0.0.1:3000`, overridable with `BETTERBOOKING_WEB_PORT`.
- API: `127.0.0.1:5245`, overridable with `BETTERBOOKING_API_PORT`.
- Local OIDC provider: `127.0.0.1:5080`, overridable with `BETTERBOOKING_LOCAL_OIDC_PORT`.
- PostgreSQL: `127.0.0.1:54329`, overridable with `BETTERBOOKING_POSTGRES_PORT`.

## Authentication

By default, Compose runs a local-only OIDC provider and configures the API and frontend to use it. The app sign-in and create-account panels let you choose:

- Traveler: books stays and manages trips.
- Property admin: creates and publishes hotel/apartment listings.

The OIDC provider is reachable at `http://localhost:5080` and issues signed local JWTs for development only. Its registration form is intentionally local-only and does not store production passwords.

To use a real external OIDC provider instead, set these environment variables before running Compose:

- `BETTERBOOKING_AUTH_ISSUER`.
- `BETTERBOOKING_AUTH_AUDIENCE`.
- `BETTERBOOKING_AUTH_CLIENT_ID`.
- `BETTERBOOKING_AUTH_CLIENT_SECRET`, only when required by the provider.
- `BETTERBOOKING_AUTH_SCOPES`.
- `BETTERBOOKING_WEB_BASE_URL`, defaults to `http://localhost:3000`.

Register this local redirect URI with the identity provider:

```text
http://localhost:3000/api/auth/callback
```

If you override `BETTERBOOKING_LOCAL_OIDC_PORT`, also set `BETTERBOOKING_LOCAL_OIDC_ISSUER` to the matching browser URL, for example `http://localhost:5081`.

## Demo Data And Payments

The `database-seed` service inserts sample published stays owned by the local property admin. This keeps the homepage useful immediately after `docker compose up --build`.

The Trips page includes a `Pay demo` action for pending reservations. This only confirms local reservation state; it does not process real cards, money movement, refunds, disputes, taxes, invoices, or provider webhooks.

Do not commit local identity-provider secrets or generated connection strings. Do not use the local OIDC provider outside development.
