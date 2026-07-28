# Auth0 Public Accounts

Use separate Auth0 applications and APIs for TEST and PROD. Never reuse client secrets across environments.

## Auth0 API

1. Create an Auth0 API for BetterBooking TEST.
2. Choose a stable Identifier, for example `https://api.test.betterbooking`.
3. Use RS256 signing.
4. Use that Identifier for both `AUTHENTICATION_AUDIENCE` and `BETTERBOOKING_AUTH_AUDIENCE`.

## Auth0 Application

1. Create a Regular Web Application.
2. Enable the Username-Password-Authentication database connection.
3. Keep public signups enabled and use Universal Login.
4. Set the token endpoint authentication method to Client Secret Post.
5. Set the application ID Token Signing Algorithm to RS256.
6. Enable RP-Initiated Logout End Session Endpoint Discovery.
7. Add exact Allowed Callback URLs:

```text
http://localhost:3000/api/auth/callback
https://<test-web-app>.azurewebsites.net/api/auth/callback
```

8. Add exact Allowed Logout URLs:

```text
http://localhost:3000/
https://<test-web-app>.azurewebsites.net/
```

9. Add the frontend origins as Allowed Web Origins.
10. Store the client secret only in GitHub environment secrets or Key Vault.

## GitHub TEST Environment

Set these environment variables:

```text
AUTHENTICATION_AUTHORITY=https://<auth0-domain>
AUTHENTICATION_AUDIENCE=<auth0-api-identifier>
FRONTEND_AUTH_CLIENT_ID=<auth0-regular-web-app-client-id>
FRONTEND_AUTH_SCOPES=openid profile email
```

Set `FRONTEND_AUTH_CLIENT_SECRET` as an environment secret.

## Security

- Enable email verification, breached-password detection, bot protection, and rate limits before public launch.
- Add a Post Login Action that denies access while `event.user.email_verified` is false. Sending a verification email alone does not block login.
- BetterBooking stores only the external issuer/subject mapping and profile fields. Auth0 stores credentials.
- Every authenticated user may book stays and create listings. Listing mutations remain owner-only in backend handlers.
- Changing the Auth0 issuer or custom domain creates a different external identity namespace; plan account migration before changing it.
