# BetterBooking Web

Next.js frontend for BetterBooking.

## Commands

```bash
npm install
npm run dev
npm run lint
npm run typecheck
npm test
npm run build
```

## Backend API

The frontend reads `BETTERBOOKING_API_BASE_URL` on the server side. If it is not set, it defaults to `http://localhost:5245`, matching the backend launch profile.

Do not place secrets in frontend environment files. Browser-exposed values must be treated as public.

## Authentication

Authentication uses OIDC authorization code flow with PKCE. Access tokens are stored only in an encrypted HttpOnly cookie and are sent to the backend from server-rendered pages.

Required runtime settings:

- `BETTERBOOKING_AUTH_ISSUER`: OIDC issuer, such as `https://your-tenant.eu.auth0.com`.
- `BETTERBOOKING_AUTH_AUDIENCE`: Auth0 API Identifier; it must match backend `Authentication:Audience`.
- `BETTERBOOKING_AUTH_CLIENT_ID`: frontend application client id.
- `BETTERBOOKING_AUTH_CLIENT_SECRET`: Auth0 Regular Web Application client secret, set only in secure local/user or hosting configuration.
- `BETTERBOOKING_AUTH_SCOPES`: OIDC scopes, normally `openid profile email`.
- `BETTERBOOKING_AUTH_COOKIE_SECRET`: high-entropy value used to encrypt the session cookie.
- `BETTERBOOKING_WEB_BASE_URL`: frontend origin used to build the callback URL outside localhost.

Register this redirect URI with the identity provider:

```text
{BETTERBOOKING_WEB_BASE_URL}/api/auth/callback
```

Register `{BETTERBOOKING_WEB_BASE_URL}/` as an allowed logout URL. See `docs/deployment/auth0.md`.

Do not commit actual values for secret-bearing settings. The backend must also be configured with matching `Authentication:Authority` and `Authentication:Audience` values.

## Mutations

Authenticated UI actions post to Next route handlers under `src/app/api`. Those handlers read the encrypted HttpOnly session cookie and call the backend with the bearer token server-side.

Current mutation flows:

- Create listing draft from `/me/listings`.
- Publish, unpublish, and archive listings from `/me/listings`.
- Request a reservation from `/listings/{listingId}` after checking availability.
- Cancel reservations from `/me/reservations`.

Do not move access-token handling into client components or browser-visible JavaScript.
