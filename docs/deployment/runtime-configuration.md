# Runtime Configuration Checklist

Do not commit runtime values for secret-bearing settings. Use local shell/user-secret storage for development and Key Vault or hosting secret stores for TEST and PROD.

## Backend API

| Setting | Required | Secret-bearing | Notes |
| --- | --- | --- | --- |
| `ConnectionStrings__ApplicationDatabase` | Yes | Yes | PostgreSQL connection string for the current environment. |
| `Authentication__Authority` | Yes for auth | No | OIDC issuer used for JWT validation. |
| `Authentication__Audience` | Yes for auth | No | Expected backend API token audience. |
| `Cors__AllowedOrigins` | Yes for browser calls | No | Must include only explicit frontend origins. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | TEST/PROD | Yes | Application Insights connection string. |

## Frontend

| Setting | Required | Secret-bearing | Notes |
| --- | --- | --- | --- |
| `BETTERBOOKING_API_BASE_URL` | Yes | No | Backend API origin reachable from the frontend server runtime. |
| `BETTERBOOKING_WEB_BASE_URL` | Yes for auth | No | Frontend origin used for OIDC redirect URI generation. |
| `BETTERBOOKING_AUTH_ISSUER` | Yes for auth | No | Same OIDC issuer as backend authority. |
| `BETTERBOOKING_AUTH_AUDIENCE` | Yes for auth | No | Auth0 API Identifier; must match the backend audience. |
| `BETTERBOOKING_AUTH_CLIENT_ID` | Yes for auth | No | Frontend application client id. |
| `BETTERBOOKING_AUTH_CLIENT_SECRET` | Provider-dependent | Yes | Only for confidential-client flows. |
| `BETTERBOOKING_AUTH_SCOPES` | Yes for auth | No | OIDC scopes, normally `openid profile email`; audience is configured separately. |
| `BETTERBOOKING_AUTH_COOKIE_SECRET` | Yes for auth | Yes | High-entropy value for encrypted HttpOnly session cookies. |

## Local Infrastructure

| Setting | Required | Secret-bearing | Notes |
| --- | --- | --- | --- |
| `BETTERBOOKING_POSTGRES_PASSWORD` | Yes | Yes | Required by `infra/local/docker-compose.yml`. |
| `BETTERBOOKING_POSTGRES_PORT` | No | No | Defaults to `54329`. |

## Verification

- Backend `/health/live` returns healthy.
- Backend `/health/ready` includes database readiness when the database is configured.
- Frontend can start sign-in and complete `/api/auth/callback`.
- Authenticated frontend pages call backend protected APIs with bearer tokens.
- OpenAPI is enabled in Development and TEST only.

## Azure Bicep Mapping

`infra/bicep/main.bicep` configures these App Service settings for one environment:

- API: `ASPNETCORE_ENVIRONMENT`, `Authentication__Authority`, `Authentication__Audience`, `Cors__AllowedOrigins__0`, `ConnectionStrings__ApplicationDatabase`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `WEBSITE_RUN_FROM_PACKAGE`.
- Frontend: `NODE_ENV`, `BETTERBOOKING_API_BASE_URL`, `BETTERBOOKING_WEB_BASE_URL`, `BETTERBOOKING_AUTH_ISSUER`, `BETTERBOOKING_AUTH_AUDIENCE`, `BETTERBOOKING_AUTH_CLIENT_ID`, `BETTERBOOKING_AUTH_SCOPES`, `BETTERBOOKING_AUTH_COOKIE_SECRET`, optional `BETTERBOOKING_AUTH_CLIENT_SECRET`, `WEBSITE_RUN_FROM_PACKAGE`.

Secret-bearing Azure settings must use Key Vault references or hosting-secret storage. TEST and PROD must use different Key Vaults and different values.

The Azure deployment workflow packages the frontend as a Next.js standalone artifact. The frontend App Service startup command is `node server.js`, so the deployed zip must have the standalone `server.js` at its root.
