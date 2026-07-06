# Security Checklist

This checklist is intentionally practical and will evolve with implementation.

## Baseline Rules

- Do not store secrets in source code, appsettings, docs, or frontend environment variables.
- Use separate TEST and PROD identities, secrets, databases, storage, Redis, and Service Bus resources.
- Never allow TEST to access PROD data, secrets, or infrastructure.
- Use HTTPS only outside local development.
- Use explicit CORS origins only.
- Use Secure, HttpOnly, SameSite cookies for browser sessions when authentication is added.
- Do not store sensitive tokens in localStorage.
- Validate all external input on the backend.
- Enforce authorization on the backend for every protected action.
- Add rate limiting to login, registration, reservation, payment, and admin endpoints.
- Do not log passwords, tokens, refresh tokens, payment data, sensitive documents, or raw sensitive payloads.

## Required Before Production

- Microsoft Entra External ID or another secure OIDC provider.
- Configure API JWT validation with production issuer and audience.
- Configure frontend OIDC runtime settings outside source control.
- Store frontend browser sessions only in encrypted Secure, HttpOnly, SameSite cookies.
- Request backend API scopes explicitly so frontend access tokens have the expected API audience.
- Map external identities to internal user ids before enabling user-facing reservation creation.
- Do not accept owner or guest user ids from browser request bodies.
- Policy-based authorization for admin and owner actions.
- Audit logging for admin actions, auth failures, reservation changes, payment events, listing changes, and account changes.
- Dependency vulnerability scanning in CI.
- Secret scanning in CI.
- SAST and IaC scanning in CI.
- File upload validation and malware scanning plan.
- Payment webhook signature validation and idempotency.
- Authorization and IDOR/BOLA tests for protected resources.
