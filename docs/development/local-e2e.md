# Local End-to-End Setup

This guide gets the local app to a real end-to-end flow without storing secrets in the repository. The default path is Docker Compose.

## Prerequisites

- .NET SDK matching `global.json`.
- Docker Desktop or another Docker-compatible runtime.
- Node.js and npm.
- Docker Compose includes a local-only OIDC provider. Use Auth0 only when you want to test external identity-provider integration.

## Identity Provider Checklist

- Register a backend API application.
- Expose a backend API scope for frontend access tokens.
- Register a frontend web application.
- Add `http://localhost:3000/api/auth/callback` as a redirect URI for local development.
- Set `BETTERBOOKING_AUTH_AUDIENCE` to the Auth0 API Identifier and use `openid profile email` scopes.
- Configure the backend JWT audience to match the access token audience issued for the API.
- Do not reuse TEST or PROD identities for local development.

## Local Secrets

The Docker Compose path generates local-only PostgreSQL and frontend cookie secrets inside a Docker named volume. Do not write real secrets into source files, docs, committed env files, or logs.

Docker Compose provides default local OIDC settings, so authenticated local smoke testing works without external identity-provider setup. Every account can book stays and manage its own listings.

Set OIDC values from a real provider only when you want to test external identity integration:

```bash
export BETTERBOOKING_AUTH_ISSUER="<local-oidc-issuer>"
export BETTERBOOKING_AUTH_AUDIENCE="<backend-api-audience>"
export BETTERBOOKING_AUTH_CLIENT_ID="<frontend-client-id>"
export BETTERBOOKING_AUTH_CLIENT_SECRET="<frontend-client-secret-if-required>"
export BETTERBOOKING_AUTH_SCOPES="openid profile email"
export BETTERBOOKING_WEB_BASE_URL="http://localhost:3000"
```

## Docker Compose Setup

From the repository root:

```bash
docker compose --file infra/local/docker-compose.yml up --build
```

Compose starts PostgreSQL, a local OIDC provider on `http://localhost:5080`, applies EF Core migrations, inserts demo listings, runs the API on `http://localhost:5245`, and runs the frontend on `http://localhost:3000`.

Stop containers while keeping local database data and generated local secrets:

```bash
docker compose --file infra/local/docker-compose.yml down
```

Delete local database data and generated local secrets only when you intentionally want a clean environment:

```bash
docker compose --file infra/local/docker-compose.yml down --volumes
```

## Manual Setup

Use this only when you need to run the API or frontend directly on the host outside Docker.

Generate local-only values in your shell session:

```bash
export BETTERBOOKING_POSTGRES_PASSWORD="$(openssl rand -base64 32)"
export BETTERBOOKING_AUTH_COOKIE_SECRET="$(openssl rand -base64 32)"
```

Set OIDC values from your local identity provider configuration in the same shell session:

```bash
export Authentication__Authority="<local-oidc-issuer>"
export Authentication__Audience="<backend-api-audience>"
export BETTERBOOKING_AUTH_ISSUER="$Authentication__Authority"
export BETTERBOOKING_AUTH_AUDIENCE="$Authentication__Audience"
export BETTERBOOKING_AUTH_CLIENT_ID="<frontend-client-id>"
export BETTERBOOKING_AUTH_CLIENT_SECRET="<frontend-client-secret-if-required>"
export BETTERBOOKING_AUTH_SCOPES="openid profile email"
export BETTERBOOKING_WEB_BASE_URL="http://localhost:3000"
export BETTERBOOKING_API_BASE_URL="http://localhost:5245"
```

## Start PostgreSQL

For host-run apps, start a separate local PostgreSQL container with your shell-provided password:

```bash
docker run --name betterbooking-postgres --detach \
  --env POSTGRES_DB=betterbooking \
  --env POSTGRES_USER=betterbooking \
  --env POSTGRES_PASSWORD="$BETTERBOOKING_POSTGRES_PASSWORD" \
  --publish "127.0.0.1:${BETTERBOOKING_POSTGRES_PORT:-54329}:5432" \
  --volume betterbooking-postgres-data:/var/lib/postgresql/data \
  postgres:17-alpine
```

The default local port is `54329`; override it with `BETTERBOOKING_POSTGRES_PORT` if needed.

Configure the backend connection string in the shell session:

```bash
export ConnectionStrings__ApplicationDatabase="Host=localhost;Port=${BETTERBOOKING_POSTGRES_PORT:-54329};Database=betterbooking;Username=betterbooking;Password=${BETTERBOOKING_POSTGRES_PASSWORD};Include Error Detail=false"
```

## Apply Migrations

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project apps/backend/src/App.Infrastructure/App.Infrastructure.csproj --startup-project apps/backend/src/App.Infrastructure/App.Infrastructure.csproj --context ApplicationDbContext
```

## Run The Apps

Run the backend API:

```bash
dotnet run --project apps/backend/src/App.Api/App.Api.csproj
```

Run the frontend in a second shell session with the same frontend auth variables:

```bash
npm install --prefix apps/web
npm run dev --prefix apps/web
```

## Manual E2E Smoke Flow

- Open `http://localhost:3000`.
- Use `Create account` to create a local identity, or use `Sign in` to choose a built-in sample identity.
- Sign in from the header.
- Any signed-in identity can create and publish listings it owns.
- Open `/me/listings`.
- Create a detailed draft listing.
- Publish the listing.
- Confirm it appears on the public search page.
- Sign out, then choose Traveler to book a stay.
- Open the listing detail page.
- Check an available date range.
- Request a reservation.
- Open `/me/reservations`.
- Use `Pay demo` to confirm the reservation. This is a local state transition only, not a real payment-provider charge.
- Cancel the reservation.
- Check the same date range again and confirm it is available.

## Local Cleanup

Stop containers while keeping local database data:

```bash
docker compose --file infra/local/docker-compose.yml down
docker stop betterbooking-postgres
```

Delete local database data only when you intentionally want a clean local database:

```bash
docker compose --file infra/local/docker-compose.yml down --volumes
docker rm betterbooking-postgres
docker volume rm betterbooking-postgres-data
```
