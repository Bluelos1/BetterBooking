# BetterBooking

BetterBooking is a security-first booking marketplace application built as a modular monolith.

## Current Direction

- Backend: ASP.NET Core, Clean Architecture style boundaries.
- Frontend: Next.js, React, TypeScript.
- Hosting target: Azure App Service with separate TEST and PROD environments.
- Secrets: never stored in source code, appsettings, docs, or local env examples.
- Production releases: staging slot first, smoke tests, then slot swap.

## Repository Layout

```text
apps/
  backend/
    src/
    tests/
  web/
infra/
docs/
```

## Local Docker Start

Run the local stack from the repository root:

```bash
docker compose --file infra/local/docker-compose.yml up --build
```

Then open `http://localhost:3000`. Compose runs PostgreSQL, a local-only OIDC provider, EF Core migrations, local demo seed data, the API on `http://localhost:5245`, and the frontend.

Authenticated local flows work through the local sign-in and create-account panels by default. Every signed-in account can book stays and publish its own apartments or hotels. Use external OIDC environment variables only when testing real identity-provider integration; see `docs/development/local-e2e.md`.

Local account creation is simulated by the Docker OIDC provider. Public account creation uses Auth0 Universal Login; BetterBooking never stores user passwords.

The local payment flow is a demo confirmation flow only. It does not charge cards or replace a real payment-provider integration.

The backend skeleton is intentionally small and buildable. Azure integrations, EF Core, Redis, Service Bus, identity, and payment providers will be added incrementally.

## Frontend

The Next.js frontend lives in `apps/web` and uses npm.

```bash
cd apps/web
npm install
npm run dev
```

Set `BETTERBOOKING_API_BASE_URL` to the backend base URL when the API is not running on `http://localhost:5245`.

Frontend authentication uses OIDC authorization code flow with PKCE. Configure the required `BETTERBOOKING_AUTH_*` runtime settings outside source control.

For local end-to-end setup, see `docs/development/local-e2e.md`.
